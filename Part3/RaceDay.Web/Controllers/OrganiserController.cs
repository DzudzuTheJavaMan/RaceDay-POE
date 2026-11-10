using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class CreatedEvent
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
}

[RoleRequired("Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api)
    {
        _api = api;
    }

    private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    private async Task<List<EventItem>> MyEvents()
    {
        var r = await _api.GetAsync("/api/events");
        var all = r.Ok ? (r.As<List<EventItem>>() ?? new List<EventItem>()) : new List<EventItem>();
        return all.Where(e => e.OrganiserId == UserId).ToList();
    }

    private async Task<EventItem?> OwnedEvent(int id)
    {
        var r = await _api.GetAsync($"/api/events/{id}");
        if (!r.Ok) return null;
        var ev = r.As<EventItem>();
        return ev != null && ev.OrganiserId == UserId ? ev : null;
    }

    private static object ToBody(EventFormModel m) => new
    {
        name = m.Name,
        description = m.Description,
        eventDate = m.EventDate,
        location = m.Location,
        distance = m.Distance,
        eventType = m.EventType
    };

    private async Task<string?> UploadBannerIfAny(int eventId, IFormFile? banner)
    {
        if (banner == null || banner.Length == 0) return null;
        var r = await _api.PostFileAsync($"/api/events/{eventId}/banner", banner);
        return r.Ok ? null : r.Message();
    }

    public async Task<IActionResult> Dashboard()
    {
        var rows = new List<DashboardRow>();
        foreach (var e in await MyEvents())
        {
            var r = await _api.GetAsync($"/api/events/{e.EventId}/enrolments");
            var count = r.Ok ? (r.As<List<OrganiserEnrolmentItem>>()?.Count ?? 0) : 0;
            rows.Add(new DashboardRow { Event = e, Enrolments = count });
        }
        return View(rows);
    }

    public async Task<IActionResult> Events() => View(await MyEvents());

    [HttpGet]
    public IActionResult CreateEvent()
    {
        ViewBag.Heading = "Create event";
        return View("EventForm", new EventFormModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormModel model, IFormFile? banner)
    {
        ViewBag.Heading = "Create event";
        if (!ModelState.IsValid) return View("EventForm", model);

        var r = await _api.PostAsync("/api/events", ToBody(model));
        if (!r.Ok)
        {
            ModelState.AddModelError("", r.Message());
            return View("EventForm", model);
        }

        var created = r.As<CreatedEvent>();
        var bannerError = created == null ? null : await UploadBannerIfAny(created.EventId, banner);

        if (bannerError != null)
            TempData["Error"] = "Event created, but the banner was not uploaded: " + bannerError;
        else
            TempData["Success"] = "Event created. Add categories so participants can enter.";

        return RedirectToAction("Events");
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await OwnedEvent(id);
        if (ev == null) return RedirectToAction("Events");

        ViewBag.Heading = "Edit event";
        ViewBag.CurrentBanner = ev.BannerImageUrl;
        return View("EventForm", new EventFormModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormModel model, IFormFile? banner)
    {
        ViewBag.Heading = "Edit event";
        if (!ModelState.IsValid) return View("EventForm", model);

        var r = await _api.PutAsync($"/api/events/{id}", ToBody(model));
        if (!r.Ok)
        {
            ModelState.AddModelError("", r.Message());
            return View("EventForm", model);
        }

        var bannerError = await UploadBannerIfAny(id, banner);

        if (bannerError != null)
            TempData["Error"] = "Event updated, but the banner was not uploaded: " + bannerError;
        else
            TempData["Success"] = "Event updated.";

        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var r = await _api.DeleteAsync($"/api/events/{id}");
        if (r.Ok) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = r.Message();
        return RedirectToAction("Events");
    }

    public async Task<IActionResult> Categories(int id)
    {
        var ev = await OwnedEvent(id);
        if (ev == null) return RedirectToAction("Events");
        return View(ev);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Enter a category name.";
            return RedirectToAction("Categories", new { id });
        }

        var r = await _api.PostAsync($"/api/events/{id}/categories", new { name });
        if (r.Ok) TempData["Success"] = "Category added.";
        else TempData["Error"] = r.Message();
        return RedirectToAction("Categories", new { id });
    }

    public async Task<IActionResult> Enrolments(int id)
    {
        var ev = await OwnedEvent(id);
        if (ev == null) return RedirectToAction("Events");

        var r = await _api.GetAsync($"/api/events/{id}/enrolments");
        ViewBag.EventName = ev.Name;
        ViewBag.EventId = id;
        return View(r.Ok ? (r.As<List<OrganiserEnrolmentItem>>() ?? new()) : new List<OrganiserEnrolmentItem>());
    }

    public async Task<IActionResult> Results(int id)
    {
        var ev = await OwnedEvent(id);
        if (ev == null) return RedirectToAction("Events");

        var r = await _api.GetAsync($"/api/events/{id}/enrolments");
        ViewBag.EventName = ev.Name;
        ViewBag.EventId = id;
        return View(r.Ok ? (r.As<List<OrganiserEnrolmentItem>>() ?? new()) : new List<OrganiserEnrolmentItem>());
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmEnrolment(int eventId, int enrolmentId)
    {
        var r = await _api.PutAsync($"/api/enrolments/{enrolmentId}/confirm");
        if (r.Ok) TempData["Success"] = "Enrolment confirmed.";
        else TempData["Error"] = r.Message();
        return RedirectToAction("Enrolments", new { id = eventId });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int eventId, int enrolmentId, string finishTime, int position)
    {
        if (!TimeSpan.TryParse(finishTime, out _) || position < 1)
        {
            TempData["Error"] = "Enter a finish time like 01:23:45 and a position of 1 or more.";
            return RedirectToAction("Results", new { id = eventId });
        }

        var r = await _api.PostAsync("/api/results", new { enrolmentId, finishTime, position });
        if (r.Ok) TempData["Success"] = "Result saved.";
        else TempData["Error"] = r.Message();
        return RedirectToAction("Results", new { id = eventId });
    }
}