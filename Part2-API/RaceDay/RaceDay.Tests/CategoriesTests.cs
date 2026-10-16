using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class CategoriesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CategoriesTests(ApiFactory factory) => _factory = factory;

    private static object Body(string name = "Under 18", int min = 10, int max = 17) =>
        new { name, minAge = min, maxAge = max };

    [Fact]
    public async Task CreateCategory_ByOwner_Returns201()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var eventId = await organiser.CreateEvent();
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", Body());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns409()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var eventId = await organiser.CreateEvent();
        await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", Body());
        var second = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", Body());
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_MaxAgeBelowMinAge_Returns400()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var eventId = await organiser.CreateEvent();
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", Body(min: 40, max: 20));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_Returns403()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var participant = await _factory.LoggedInAs("Participant");
        var eventId = await organiser.CreateEvent();
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", Body());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ByAnotherOrganiser_Returns403()
    {
        var owner = await _factory.LoggedInAs("Organiser");
        var other = await _factory.LoggedInAs("Organiser");
        var eventId = await owner.CreateEvent();
        var response = await other.PostAsJsonAsync($"/api/events/{eventId}/categories", Body());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_UnknownEvent_Returns404()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events/99999/categories", Body());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_Returns200()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var eventId = await organiser.CreateEvent();
        await organiser.CreateCategory(eventId);
        var response = await organiser.GetAsync($"/api/events/{eventId}/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ByOwner_Returns200()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var eventId = await organiser.CreateEvent();
        var categoryId = await organiser.CreateCategory(eventId);
        var response = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", Body("Junior"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithoutEnrolments_Returns204()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var eventId = await organiser.CreateEvent();
        var categoryId = await organiser.CreateCategory(eventId);
        var response = await organiser.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolment_Returns409()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var participant = await _factory.LoggedInAs("Participant");
        var eventId = await organiser.CreateEvent();
        var categoryId = await organiser.CreateCategory(eventId);
        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var response = await organiser.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}