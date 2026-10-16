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

    /// <summary>Finish time as hh:mm:ss, for example 03:45:10.</summary>
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int Position { get; set; }
}

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    /// <summary>Captures the finish time and finishing position for a Participant's enrolment in an event owned by the logged-in Organiser.</summary>
    /// <remarks>The enrolment must belong to the event. Each enrolment can only have one result.</remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid data or enrolment does not belong to this event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the event owner, or not an Organiser.</response>
    /// <response code="404">Event or enrolment does not exist.</response>
    /// <response code="409">Result already captured for this enrolment.</response>
    [HttpPost("events/{id}/results")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(int id, ResultRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(403, new { message = "You do not own this event." });

        if (request.FinishTime <= TimeSpan.Zero)
            return BadRequest(new { message = "Finish time must be greater than zero." });

        var enrolment = await _db.Enrolments.FindAsync(request.EnrolmentId);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment does not exist." });
        if (enrolment.EventId != id)
            return BadRequest(new { message = "Enrolment does not belong to this event." });

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

    /// <summary>Lists the logged-in Participant's own race results across all events.</summary>
    /// <response code="200">List of results with event, category, finish time and position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("results/me")]
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
                r.Enrolment.EventId,
                EventName = r.Enrolment.Event.Name,
                r.Enrolment.Event.EventDate,
                CategoryName = r.Enrolment.Category.Name,
                r.FinishTime,
                r.Position
            })
            .ToListAsync();

        return Ok(list);
    }
}