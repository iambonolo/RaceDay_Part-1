using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EventsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EventsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetEvents_NotLoggedIn_Returns401()
    {
        var response = await _factory.NewClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_LoggedIn_Returns200()
    {
        var client = await _factory.LoggedInAs("Participant");
        var response = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var participant = await _factory.LoggedInAs("Participant");
        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_NotLoggedIn_Returns401()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/events", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.EventBody("Swim"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_DateInThePast_Returns400()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.EventBody(daysAhead: -5));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEventById_Existing_Returns200()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var id = await organiser.CreateEvent();
        var response = await organiser.GetAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEventById_Unknown_Returns404()
    {
        var client = await _factory.LoggedInAs("Participant");
        var response = await client.GetAsync("/api/events/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByOwner_Returns200()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var id = await organiser.CreateEvent();
        var response = await organiser.PutAsJsonAsync($"/api/events/{id}", TestHelpers.EventBody("Walk"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByAnotherOrganiser_Returns403()
    {
        var owner = await _factory.LoggedInAs("Organiser");
        var other = await _factory.LoggedInAs("Organiser");
        var id = await owner.CreateEvent();
        var response = await other.PutAsJsonAsync($"/api/events/{id}", TestHelpers.EventBody("Walk"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithoutCategories_Returns204()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var id = await organiser.CreateEvent();
        var response = await organiser.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithCategories_Returns409()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var id = await organiser.CreateEvent();
        await organiser.CreateCategory(id);
        var response = await organiser.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByParticipant_Returns403()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var participant = await _factory.LoggedInAs("Participant");
        var id = await organiser.CreateEvent();
        var response = await participant.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}