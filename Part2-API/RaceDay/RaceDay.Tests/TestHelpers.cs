using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public const string Password = "StrongPass1!";

    public static HttpClient NewClient(this ApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    public static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    public static Task<HttpResponseMessage> Register(this HttpClient client, string email, string role, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/register", new { fullName = "Test User", email, password, role });

    public static Task<HttpResponseMessage> Login(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<HttpClient> LoggedInAs(this ApiFactory factory, string role)
    {
        var client = factory.NewClient();
        var email = NewEmail();
        (await client.Register(email, role)).EnsureSuccessStatusCode();
        (await client.Login(email)).EnsureSuccessStatusCode();
        return client;
    }

    public static object EventBody(string eventType = "Run", int daysAhead = 30) => new
    {
        name = "Test 10K",
        description = "A test event",
        eventDate = DateTime.UtcNow.AddDays(daysAhead).ToString("yyyy-MM-dd"),
        location = "Johannesburg",
        distanceKm = 10,
        eventType
    };

    public static async Task<int> ReadId(this HttpResponseMessage response, string property)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }

    public static async Task<int> CreateEvent(this HttpClient organiser) =>
        await (await organiser.PostAsJsonAsync("/api/events", EventBody())).ReadId("eventId");

    public static async Task<int> CreateCategory(this HttpClient organiser, int eventId, string name = "Open") =>
        await (await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name, minAge = 18, maxAge = 99 })).ReadId("categoryId");
}