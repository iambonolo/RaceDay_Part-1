using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[SessionAuthorize]
public class UsersController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns the logged-in user's profile.</summary>
    /// <response code="200">The profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Fail(StatusCodes.Status401Unauthorized, "Your account no longer exists. Please log in again.");
        }
        return Ok(user.ToProfile());
    }

    /// <summary>Updates the logged-in user's name and email.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">Email already used by another account.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Fail(StatusCodes.Status401Unauthorized, "Your account no longer exists. Please log in again.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != user.UserId))
        {
            return Fail(StatusCodes.Status409Conflict, "Another account already uses this email.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(user.ToProfile());
    }
}