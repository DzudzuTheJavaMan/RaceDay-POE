using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EnrolmentsController(RaceDayContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    /// <summary>Enrols the logged-in Participant into an event under a selected category.</summary>
    /// <remarks>The category must belong to the event. A Participant can only enrol once per event.</remarks>
    /// <response code="201">Enrolment recorded with status Pending.</response>
    /// <response code="400">Category does not belong to the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">Event or category does not exist.</response>
    /// <response code="409">Already enrolled.</response>
    [HttpPost("enrolments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolmentRequest request)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == request.EventId))
            return NotFound(new { message = "Event does not exist." });

        var category = await _db.Categories.FindAsync(request.CategoryId);
        if (category == null)
            return NotFound(new { message = "Category does not exist." });
        if (category.EventId != request.EventId)
            return BadRequest(new { message = "Category does not belong to this event." });

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == CurrentUserId && e.EventId == request.EventId))
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = CurrentUserId,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            Status = "Pending"
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            enrolment.EnrolmentId, enrolment.EventId, enrolment.CategoryId, enrolment.Status
        });
    }

    /// <summary>Lists all enrolments for the logged-in Participant.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("enrolments/me")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyEnrolments()
    {
        var list = await _db.Enrolments
            .Where(e => e.ParticipantId == CurrentUserId)
            .OrderBy(e => e.Event.EventDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EventId,
                EventName = e.Event.Name,
                e.Event.EventDate,
                e.CategoryId,
                CategoryName = e.Category.Name,
                e.Status,
                e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Lists all enrolments for an event owned by the logged-in Organiser.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the event owner, or not an Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{id}/enrolments")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EventEnrolments(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(403, new { message = "You do not own this event." });

        var list = await _db.Enrolments
            .Where(e => e.EventId == id)
            .Select(e => new
            {
                e.EnrolmentId,
                e.ParticipantId,
                ParticipantName = e.Participant.FullName,
                ParticipantEmail = e.Participant.Email,
                e.CategoryId,
                CategoryName = e.Category.Name,
                e.Status,
                e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }
}