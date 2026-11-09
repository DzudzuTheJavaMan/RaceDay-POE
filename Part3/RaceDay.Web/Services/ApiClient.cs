using System.Net;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult
{
    public HttpStatusCode Status { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool Ok => (int)Status >= 200 && (int)Status < 300;

    public T? As<T>() =>
        string.IsNullOrWhiteSpace(Body) ? default : JsonSerializer.Deserialize<T>(Body, ApiClient.Json);

    public string Message()
    {
        try
        {
            using var doc = JsonDocument.Parse(Body);
            if (doc.RootElement.TryGetProperty("message", out var m))
                return m.GetString() ?? "Something went wrong.";
        }
        catch { }
        return Ok ? "" : "Something went wrong. Please check your input and try again.";
    }
}

public class ApiClient
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _ctx;

    public ApiClient(IHttpClientFactory factory, IHttpContextAccessor ctx)
    {
        _factory = factory;
        _ctx = ctx;
    }

    private ISession Session => _ctx.HttpContext!.Session;

    public async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var client = _factory.CreateClient("api");
        var request = new HttpRequestMessage(method, url);

        var cookie = Session.GetString("ApiCookie");
        if (!string.IsNullOrEmpty(cookie)) request.Headers.Add("Cookie", cookie);

        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        try
        {
            var response = await client.SendAsync(request);

            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                var session = cookies.FirstOrDefault(c => c.StartsWith(".AspNetCore.Session="));
                if (session != null) Session.SetString("ApiCookie", session.Split(';')[0]);
            }

            return new ApiResult { Status = response.StatusCode, Body = await response.Content.ReadAsStringAsync() };
        }
        catch (HttpRequestException)
        {
            return new ApiResult
            {
                Status = HttpStatusCode.ServiceUnavailable,
                Body = "{\"message\":\"The service is unavailable right now. Please try again later.\"}"
            };
        }
    }

    public Task<ApiResult> GetAsync(string url) => SendAsync(HttpMethod.Get, url);
    public Task<ApiResult> PostAsync(string url, object? body = null) => SendAsync(HttpMethod.Post, url, body);
    public Task<ApiResult> PutAsync(string url, object? body = null) => SendAsync(HttpMethod.Put, url, body);
    public Task<ApiResult> DeleteAsync(string url) => SendAsync(HttpMethod.Delete, url);
}