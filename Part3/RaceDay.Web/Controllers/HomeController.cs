using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? search, string? type)
    {
        var result = await _api.GetAsync("/api/events");
        var events = result.Ok
            ? (result.As<List<EventItem>>() ?? new List<EventItem>())
            : new List<EventItem>();

        if (!result.Ok) ViewBag.Error = result.Message();

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType == type).ToList();

        return View(new HomeIndexViewModel { Events = events, Search = search, Type = type });
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetAsync($"/api/events/{id}");
        if (!result.Ok)
        {
            TempData["Error"] = result.Message();
            return RedirectToAction("Index");
        }

        return View(result.As<EventItem>());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}