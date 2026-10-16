using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Starts the real API in memory, with an in-memory database instead of SQL Server.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<RaceDayContext>) ||
                d.ServiceType == typeof(RaceDayContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class TestHelpers
{
    public const string Password = "Test1234";

    public static Task<HttpResponseMessage> Register(HttpClient client, string email, string role, string password = Password)
        => client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test User", email, password, role });

    public static Task<HttpResponseMessage> Login(HttpClient client, string email, string password = Password)
        => client.PostAsJsonAsync("/api/auth/login", new { email, password });

    /// <summary>Registers a user and logs them in. Each client has its own session cookie.</summary>
    public static async Task<HttpClient> LoggedInClient(ApiFactory factory, string email, string role)
    {
        var client = factory.CreateClient();
        await Register(client, email, role);
        await Login(client, email);
        return client;
    }

    public static object NewEvent(string eventType = "Run") => new
    {
        name = "Test Marathon",
        description = "A test event",
        eventDate = DateTime.Today.AddDays(30),
        location = "Johannesburg",
        distance = 21.1,
        eventType
    };

    public static async Task<int> ReadId(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }
}