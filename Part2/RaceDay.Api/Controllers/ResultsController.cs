using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class ResultRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int Position { get; set; }
}

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    /// <summary>Captures a finish time and finishing position for a Participant's enrolment.</summary>
    /// <remarks>Only the Organiser who owns the event can capture its results. FinishTime is sent as hh:mm:ss.</remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the event owner, or not an Organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">Result already captured.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(ResultRequest request)
    {
        if (request.FinishTime <= TimeSpan.Zero)
            return BadRequest(new { message = "Finish time must be greater than zero." });

        var enrolment = await _db.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment does not exist." });
        if (enrolment.Event.OrganiserId != CurrentUserId)
            return StatusCode(403, new { message = "You do not own this event." });
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { result.ResultId, result.EnrolmentId, result.FinishTime, result.Position });
    }

    /// <summary>Lists the logged-in Participant's personal race history.</summary>
    /// <remarks>Includes event name, date, category, finish time, position and the total number of finishers for that event.</remarks>
    /// <response code="200">List of results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("me")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyResults()
    {
        var list = await _db.Results
            .Where(r => r.Enrolment.ParticipantId == CurrentUserId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .Select(r => new
            {
                r.ResultId,
                EventName = r.Enrolment.Event.Name,
                EventDate = r.Enrolment.Event.EventDate,
                CategoryName = r.Enrolment.Category.Name,
                r.FinishTime,
                r.Position,
                TotalFinishers = _db.Results.Count(x => x.Enrolment.EventId == r.Enrolment.EventId)
            })
            .ToListAsync();

        return Ok(list);
    }
}