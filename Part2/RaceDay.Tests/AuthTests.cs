using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public class AuthTests
{
    [Fact]
    public async Task Register_NewParticipant_Returns201()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await TestHelpers.Register(client, "runner@test.com", "Participant");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_NewOrganiser_Returns201()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await TestHelpers.Register(client, "boss@test.com", "Organiser");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await TestHelpers.Register(client, "same@test.com", "Participant");

        var response = await TestHelpers.Register(client, "same@test.com", "Participant");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await TestHelpers.Register(client, "admin@test.com", "Admin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await TestHelpers.Register(client, "short@test.com", "Participant", "123");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await TestHelpers.Register(client, "hash@test.com", "Participant");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        var user = db.Users.Single(u => u.Email == "hash@test.com");

        Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
        Assert.Contains(":", user.PasswordHash);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await TestHelpers.Register(client, "login@test.com", "Participant");

        var response = await TestHelpers.Login(client, "login@test.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await TestHelpers.Register(client, "wrong@test.com", "Participant");

        var response = await TestHelpers.Login(client, "wrong@test.com", "WrongPass1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await TestHelpers.Login(client, "nobody@test.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_NotLoggedIn_Returns401()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_LoggedIn_Returns200_ThenSessionIsGone()
    {
        using var factory = new ApiFactory();
        var client = await TestHelpers.LoggedInClient(factory, "out@test.com", "Participant");

        var first = await client.PostAsync("/api/auth/logout", null);
        var second = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }
}