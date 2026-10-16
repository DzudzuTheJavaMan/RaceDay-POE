using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public static class Setup
{
    /// <summary>Creates a logged-in Organiser who owns one event with one category.</summary>
    public static async Task<(HttpClient organiser, int eventId, int categoryId)> EventWithCategory(
        ApiFactory factory, string organiserEmail = "organiser@test.com")
    {
        var organiser = await TestHelpers.LoggedInClient(factory, organiserEmail, "Organiser");

        var eventResponse = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEvent());
        var eventId = await TestHelpers.ReadId(eventResponse, "eventId");

        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "21km" });
        var categoryId = await TestHelpers.ReadId(categoryResponse, "categoryId");

        return (organiser, eventId, categoryId);
    }

    public static Task<HttpResponseMessage> Enrol(HttpClient participant, int eventId, int categoryId)
        => participant.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });
}

public class EventTests
{
    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        using var factory = new ApiFactory();
        var organiser = await TestHelpers.LoggedInClient(factory, "org@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        using var factory = new ApiFactory();
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.NewEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_NotLoggedIn_Returns401()
    {
        using var factory = new ApiFactory();
        var anonymous = factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/events", TestHelpers.NewEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        using var factory = new ApiFactory();
        var organiser = await TestHelpers.LoggedInClient(factory, "org@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEvent("Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_Anonymous_Returns200()
    {
        using var factory = new ApiFactory();
        var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_OwnEvent_Returns200()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, _) = await Setup.EventWithCategory(factory);

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", TestHelpers.NewEvent("Walk"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_OtherOrganiser_Returns403()
    {
        using var factory = new ApiFactory();
        var (_, eventId, _) = await Setup.EventWithCategory(factory);
        var otherOrganiser = await TestHelpers.LoggedInClient(factory, "other@test.com", "Organiser");

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", TestHelpers.NewEvent("Walk"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_OwnEvent_Returns200_ThenEventIsGone()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, _) = await Setup.EventWithCategory(factory);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        var get = await organiser.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");
        await Setup.Enrol(participant, eventId, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_AsOrganiser_Returns201()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, _) = await Setup.EventWithCategory(factory);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "Under 20" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_AsParticipant_Returns403()
    {
        using var factory = new ApiFactory();
        var (_, eventId, _) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "Senior" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class EnrolmentTests
{
    [Fact]
    public async Task Enrol_AsParticipant_Returns201_AndIsRecordedAsPending()
    {
        using var factory = new ApiFactory();
        var (_, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");

        var enrol = await Setup.Enrol(participant, eventId, categoryId);
        var mine = await participant.GetAsync("/api/enrolments/me");
        var list = await mine.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(eventId, list[0].GetProperty("eventId").GetInt32());
        Assert.Equal(categoryId, list[0].GetProperty("categoryId").GetInt32());
        Assert.Equal("Pending", list[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, categoryId) = await Setup.EventWithCategory(factory);

        var response = await Setup.Enrol(organiser, eventId, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_NotLoggedIn_Returns401()
    {
        using var factory = new ApiFactory();
        var (_, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var anonymous = factory.CreateClient();

        var response = await Setup.Enrol(anonymous, eventId, categoryId);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        using var factory = new ApiFactory();
        var (_, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");
        await Setup.Enrol(participant, eventId, categoryId);

        var response = await Setup.Enrol(participant, eventId, categoryId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryFromDifferentEvent_Returns400()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, _) = await Setup.EventWithCategory(factory);

        var secondEvent = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEvent("Cycle"));
        var secondEventId = await TestHelpers.ReadId(secondEvent, "eventId");
        var otherCategory = await organiser.PostAsJsonAsync($"/api/events/{secondEventId}/categories", new { name = "50km" });
        var otherCategoryId = await TestHelpers.ReadId(otherCategory, "categoryId");

        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");
        var response = await Setup.Enrol(participant, eventId, otherCategoryId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownEvent_Returns404()
    {
        using var factory = new ApiFactory();
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");

        var response = await Setup.Enrol(participant, 999, 1);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EventEnrolments_OwnerOrganiser_Returns200_WithParticipant()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");
        await Setup.Enrol(participant, eventId, categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("runner@test.com", list[0].GetProperty("participantEmail").GetString());
    }

    [Fact]
    public async Task EventEnrolments_AsParticipant_Returns403()
    {
        using var factory = new ApiFactory();
        var (_, eventId, _) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventEnrolments_OtherOrganiser_Returns403()
    {
        using var factory = new ApiFactory();
        var (_, eventId, _) = await Setup.EventWithCategory(factory);
        var otherOrganiser = await TestHelpers.LoggedInClient(factory, "other@test.com", "Organiser");

        var response = await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class ResultTests
{
    private static async Task<int> EnrolmentIdFor(HttpClient participant, int eventId, int categoryId)
    {
        var response = await Setup.Enrol(participant, eventId, categoryId);
        return await TestHelpers.ReadId(response, "enrolmentId");
    }

    [Fact]
    public async Task CaptureResult_AsOrganiser_Returns201()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");
        var enrolmentId = await EnrolmentIdFor(participant, eventId, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/results",
            new { enrolmentId, finishTime = "01:45:30", position = 12 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403()
    {
        using var factory = new ApiFactory();
        var (_, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");
        var enrolmentId = await EnrolmentIdFor(participant, eventId, categoryId);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/results",
            new { enrolmentId, finishTime = "01:45:30", position = 12 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_AsParticipant_ReturnsOwnResult()
    {
        using var factory = new ApiFactory();
        var (organiser, eventId, categoryId) = await Setup.EventWithCategory(factory);
        var participant = await TestHelpers.LoggedInClient(factory, "runner@test.com", "Participant");
        var enrolmentId = await EnrolmentIdFor(participant, eventId, categoryId);
        await organiser.PostAsJsonAsync($"/api/events/{eventId}/results",
            new { enrolmentId, finishTime = "01:45:30", position = 12 });

        var response = await participant.GetAsync("/api/results/me");
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(12, list[0].GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task MyResults_AsOrganiser_Returns403()
    {
        using var factory = new ApiFactory();
        var organiser = await TestHelpers.LoggedInClient(factory, "org@test.com", "Organiser");

        var response = await organiser.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}