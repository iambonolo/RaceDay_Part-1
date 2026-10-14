using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Api.Auth;

namespace RaceDay.Api.Controllers;

public abstract class RaceDayControllerBase : ControllerBase
{
    // The logged-in user's ID, read from the server-side session.
    protected int CurrentUserId =>
        HttpContext.Session.GetUserId() ?? throw new InvalidOperationException("No user is logged in.");

    // Returns a consistent error response such as 404 Not Found or 409 Conflict.
    protected ObjectResult Fail(int statusCode, string detail) =>
        Problem(detail: detail, statusCode: statusCode, title: ReasonPhrases.GetReasonPhrase(statusCode));
}