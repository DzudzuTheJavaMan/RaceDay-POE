using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] ValidTypes = { "Run", "Walk", "Cycle" };
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    /// <summary>Lists all upcoming events. Public.</summary>
    /// <response code="200">List of upcoming events.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
            .Where(e => e.EventDate >= DateTime.Today)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId, e.Name, e.Description, e.EventDate,
                e.Location, e.Distance, e.EventType, e.BannerImageUrl, e.OrganiserId
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Gets the full details of one event, including its categories. Public.</summary>
    /// <response code="200">Event details.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events
            .Where(e => e.EventId == id)
            .Select(e => new
            {
                e.EventId, e.Name, e.Description, e.EventDate,
                e.Location, e.Distance, e.EventType, e.BannerImageUrl, e.OrganiserId,
                Categories = e.Categories.Select(c => new { c.CategoryId, c.Name })
            })
            .FirstOrDefaultAsync();

        if (ev == null) return NotFound(new { message = "Event does not exist." });
        return Ok(ev);
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>EventType must be Run, Walk or Cycle.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        if (!ValidTypes.Contains(request.EventType))
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = request.EventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, new { ev.EventId, ev.Name });
    }

    /// <summary>Updates an event owned by the logged-in Organiser.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="403">Not the event owner, or not an Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(403, new { message = "You do not own this event." });
        if (!ValidTypes.Contains(request.EventType))
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.Distance = request.Distance;
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Event updated." });
    }

    /// <summary>Deletes an event owned by the logged-in Organiser, along with its categories.</summary>
    /// <remarks>An event that already has enrolments cannot be deleted.</remarks>
    /// <response code="200">Event deleted.</response>
    /// <response code="403">Not the event owner, or not an Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">Event has enrolments.</response>
    [HttpDelete("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Categories)
            .Include(e => e.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null) return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(403, new { message = "You do not own this event." });
        if (ev.Enrolments.Any())
            return Conflict(new { message = "This event has enrolments and cannot be deleted." });

        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Event deleted." });
    }

    /// <summary>Lists the categories available for an event. Public.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id))
            return NotFound(new { message = "Event does not exist." });

        var categories = await _db.Categories
            .Where(c => c.EventId == id)
            .Select(c => new { c.CategoryId, c.Name })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds an age or distance category to an event owned by the logged-in Organiser.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="403">Not the event owner, or not an Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPost("{id}/categories")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCategory(int id, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(403, new { message = "You do not own this event." });

        var category = new Category { EventId = id, Name = request.Name.Trim() };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { category.CategoryId, category.EventId, category.Name });
    }
}