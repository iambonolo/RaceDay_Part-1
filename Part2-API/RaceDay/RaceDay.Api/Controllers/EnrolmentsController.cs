using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class EnrolmentsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    private IQueryable<Enrolment> EnrolmentsWithDetails() =>
        _db.Enrolments.Include(e => e.Event).Include(e => e.Category).Include(e => e.Participant);

    /// <summary>Enrols the logged-in Participant in an event category.</summary>
    /// <response code="201">Enrolled.</response>
    /// <response code="400">Category does not belong to this event, or the event has passed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolRequest request)
    {
        var raceEvent = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Event not found.");
        }
        if (raceEvent.EventDate.Date < DateTime.UtcNow.Date)
        {
            return Fail(StatusCodes.Status400BadRequest, "This event has already taken place.");
        }
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId))
        {
            return Fail(StatusCodes.Status400BadRequest, "That category does not belong to this event.");
        }
        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == CurrentUserId && e.EventId == eventId))
        {
            return Fail(StatusCodes.Status409Conflict, "You are already enrolled in this event.");
        }

        var enrolment = new Enrolment
        {
            ParticipantId = CurrentUserId,
            EventId = eventId,
            CategoryId = request.CategoryId,
            DateEnrolled = DateTime.UtcNow,
            EnrolmentStatus = "Confirmed"
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var saved = await EnrolmentsWithDetails().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created("/api/users/me/enrolments", saved.ToResponse());
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">The enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("users/me/enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyEnrolments()
    {
        var list = await EnrolmentsWithDetails()
            .Where(e => e.ParticipantId == CurrentUserId)
            .OrderBy(e => e.Event!.EventDate)
            .ToListAsync();
        return Ok(list.Select(e => e.ToResponse()));
    }

    /// <summary>Lists everyone enrolled in an event. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">The enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EventEnrolments(int eventId)
    {
        var raceEvent = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Event not found.");
        }
        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Fail(StatusCodes.Status403Forbidden, "You can only view enrolments for events that you created.");
        }

        var list = await EnrolmentsWithDetails()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.DateEnrolled)
            .ToListAsync();
        return Ok(list.Select(e => e.ToResponse()));
    }

    /// <summary>Records a result for an enrolment. Only the Organiser who owns the event can do this.</summary>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid finish time (use hh:mm:ss).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("enrolments/{id:int}/results")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddResult(int id, [FromBody] ResultRequest request)
    {
        var enrolment = await EnrolmentsWithDetails().FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Enrolment not found.");
        }
        if (enrolment.Event!.OrganiserId != CurrentUserId)
        {
            return Fail(StatusCodes.Status403Forbidden, "You can only record results for events that you created.");
        }
        if (!TimeSpan.TryParseExact(request.FinishTime, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var finishTime)
            || finishTime <= TimeSpan.Zero)
        {
            return Fail(StatusCodes.Status400BadRequest, "finishTime must be in hh:mm:ss format and greater than zero.");
        }
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
        {
            return Fail(StatusCodes.Status409Conflict, "A result has already been recorded for this enrolment.");
        }

        var result = new Result
        {
            EnrolmentId = id,
            FinishTime = finishTime,
            FinishPosition = request.FinishPosition,
            DateCaptured = DateTime.UtcNow
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();
        result.Enrolment = enrolment;

        return Created("/api/users/me/results", result.ToResponse());
    }

    /// <summary>Lists the logged-in Participant's own results.</summary>
    /// <response code="200">The results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("users/me/results")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyResults()
    {
        var list = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Where(r => r.Enrolment!.ParticipantId == CurrentUserId)
            .OrderByDescending(r => r.DateCaptured)
            .ToListAsync();
        return Ok(list.Select(r => r.ToResponse()));
    }
}