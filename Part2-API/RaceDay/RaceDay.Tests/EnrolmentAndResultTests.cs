using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EnrolmentAndResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EnrolmentAndResultTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Organiser, HttpClient Participant, int EventId, int CategoryId)> SetUp()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var participant = await _factory.LoggedInAs("Participant");
        var eventId = await organiser.CreateEvent();
        var categoryId = await organiser.CreateCategory(eventId);
        return (organiser, participant, eventId, categoryId);
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201()
    {
        var (_, participant, eventId, categoryId) = await SetUp();
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var (_, participant, eventId, categoryId) = await SetUp();
        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var (organiser, _, eventId, categoryId) = await SetUp();
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_NotLoggedIn_Returns401()
    {
        var (_, _, eventId, categoryId) = await SetUp();
        var response = await _factory.NewClient().PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_Returns400()
    {
        var (organiser, participant, eventId, _) = await SetUp();
        var otherEventId = await organiser.CreateEvent();
        var otherCategoryId = await organiser.CreateCategory(otherEventId);
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new { categoryId = otherCategoryId });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownEvent_Returns404()
    {
        var participant = await _factory.LoggedInAs("Participant");
        var response = await participant.PostAsJsonAsync("/api/events/99999/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrolments_AsParticipant_Returns200()
    {
        var (_, participant, eventId, categoryId) = await SetUp();
        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var response = await participant.GetAsync("/api/users/me/enrolments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrolments_AsOrganiser_Returns403()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var response = await organiser.GetAsync("/api/users/me/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrolments_ByOwner_Returns200()
    {
        var (organiser, _, eventId, _) = await SetUp();
        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrolments_ByParticipant_Returns403()
    {
        var (_, participant, eventId, _) = await SetUp();
        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddResult_ByOwner_Returns201()
    {
        var (organiser, participant, eventId, categoryId) = await SetUp();
        var enrolmentId = await (await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", new { categoryId })).ReadId("enrolmentId");
        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:30", finishPosition = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddResult_InvalidTimeFormat_Returns400()
    {
        var (organiser, participant, eventId, categoryId) = await SetUp();
        var enrolmentId = await (await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", new { categoryId })).ReadId("enrolmentId");
        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "fast", finishPosition = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddResult_Twice_Returns409()
    {
        var (organiser, participant, eventId, categoryId) = await SetUp();
        var enrolmentId = await (await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", new { categoryId })).ReadId("enrolmentId");
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:30", finishPosition = 1 });
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:50:00", finishPosition = 2 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AddResult_ByParticipant_Returns403()
    {
        var (_, participant, eventId, categoryId) = await SetUp();
        var enrolmentId = await (await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", new { categoryId })).ReadId("enrolmentId");
        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:30", finishPosition = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddResult_UnknownEnrolment_Returns404()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var response = await organiser.PostAsJsonAsync("/api/enrolments/99999/results",
            new { finishTime = "00:52:30", finishPosition = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_ShowsRecordedResult()
    {
        var (organiser, participant, eventId, categoryId) = await SetUp();
        var enrolmentId = await (await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", new { categoryId })).ReadId("enrolmentId");
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:30", finishPosition = 1 });

        var response = await participant.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("00:52:30", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetMyResults_AsOrganiser_Returns403()
    {
        var organiser = await _factory.LoggedInAs("Organiser");
        var response = await organiser.GetAsync("/api/users/me/results");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}