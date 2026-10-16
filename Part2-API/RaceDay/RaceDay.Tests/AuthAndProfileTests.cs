using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class AuthAndProfileTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthAndProfileTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var response = await _factory.NewClient().Register(TestHelpers.NewEmail(), "Participant");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        var response = await _factory.NewClient().Register(TestHelpers.NewEmail(), "Organiser");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.NewEmail();
        await client.Register(email, "Participant");
        var second = await client.Register(email, "Participant");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var response = await _factory.NewClient().Register(TestHelpers.NewEmail(), "Admin");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await _factory.NewClient().Register(TestHelpers.NewEmail(), "Participant", "short");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await _factory.NewClient().Register("not-an-email", "Participant");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ResponseDoesNotExposePasswordOrHash()
    {
        var response = await _factory.NewClient().Register(TestHelpers.NewEmail(), "Participant");
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PBKDF2", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.NewEmail();
        await client.Register(email, "Participant");
        var response = await client.Login(email);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.NewEmail();
        await client.Register(email, "Participant");
        var response = await client.Login(email, "WrongPassword1!");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _factory.NewClient().Login("nobody@test.com");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenLoggedIn_Returns200()
    {
        var client = await _factory.LoggedInAs("Participant");
        var response = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenNotLoggedIn_Returns401()
    {
        var response = await _factory.NewClient().PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AfterLogout_ProtectedEndpointReturns401()
    {
        var client = await _factory.LoggedInAs("Participant");
        await client.PostAsync("/api/auth/logout", null);
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_NotLoggedIn_Returns401()
    {
        var response = await _factory.NewClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_LoggedIn_Returns200()
    {
        var client = await _factory.LoggedInAs("Participant");
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_ValidChange_Returns200()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.NewEmail();
        await client.Register(email, "Participant");
        await client.Login(email);
        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "New Name", email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("New Name", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UpdateMe_EmailUsedByAnotherUser_Returns409()
    {
        var other = _factory.NewClient();
        var otherEmail = TestHelpers.NewEmail();
        await other.Register(otherEmail, "Participant");

        var client = await _factory.LoggedInAs("Participant");
        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Someone", email = otherEmail });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}