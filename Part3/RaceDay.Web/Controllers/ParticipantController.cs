using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleRequired("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Enrolments()
    {
        var result = await _api.GetAsync("/api/enrolments/me");
        if (!result.Ok)
        {
            ViewBag.Error = result.Message();
            return View(new List<EnrolmentItem>());
        }

        return View(result.As<List<EnrolmentItem>>() ?? new List<EnrolmentItem>());
    }

    public async Task<IActionResult> Results()
    {
        var result = await _api.GetAsync("/api/results/me");
        if (!result.Ok)
        {
            ViewBag.Error = result.Message();
            return View(new List<ResultItem>());
        }

        return View(result.As<List<ResultItem>>() ?? new List<ResultItem>());
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await _api.PostAsync("/api/enrolments", new { eventId, categoryId });

        if (result.Ok)
        {
            TempData["Success"] = "You are enrolled. Your enrolment is pending.";
            return RedirectToAction("Enrolments");
        }

        TempData["Error"] = result.Message();
        return RedirectToAction("Details", "Home", new { id = eventId });
    }
}