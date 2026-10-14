using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(RaceDayDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    /// <summary>Registers a new user as an Organiser or a Participant.</summary>
    /// <remarks>
    /// The role must be "Organiser" or "Participant". The password is hashed before it is stored.
    /// If the server is configured with Registration:OrganiserCode, Organisers must supply it.
    /// </remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Invalid input or invalid role.</response>
    /// <response code="403">Organiser code missing or wrong.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!RoleNames.TryNormalise(request.Role, out var roleName))
        {
            return Fail(StatusCodes.Status400BadRequest, "Role must be either 'Organiser' or 'Participant'.");
        }

        var requiredCode = _configuration["Registration:OrganiserCode"];
        if (roleName == RoleNames.Organiser
            && !string.IsNullOrWhiteSpace(requiredCode)
            && request.OrganiserCode != requiredCode)
        {
            return Fail(StatusCodes.Status403Forbidden, "A valid organiser code is required to register as an Organiser.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Fail(StatusCodes.Status409Conflict, "An account with this email already exists.");
        }

        var role = await _db.Roles.SingleAsync(r => r.RoleName == roleName);

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            RoleId = role.RoleId,
            DateJoined = DateTime.UtcNow
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Fail(StatusCodes.Status409Conflict, "An account with this email already exists.");
        }

        user.Role = role;
        return Created("/api/users/me", user.ToProfile());
    }

    /// <summary>Logs a user in and starts a server-side session.</summary>
    /// <remarks>On success the session stores the user's ID and role. The browser keeps a session cookie.</remarks>
    /// <response code="200">Logged in. Returns the user's profile.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Fail(StatusCodes.Status401Unauthorized, "Invalid email or password.");
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role!.RoleName);

        return Ok(user.ToProfile());
    }

    /// <summary>Ends the current user's session.</summary>
    /// <response code="200">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("Logged out."));
    }
}