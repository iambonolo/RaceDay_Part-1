using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all events, soonest first. Any logged-in user can use this.</summary>
    /// <param name="eventType">Optional filter: Run, Walk or Cycle.</param>
    /// <response code="200">The list of events.</response>
    /// <response code="400">Invalid eventType filter.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [SessionAuthorize]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] string? eventType)
    {
        var query = _db.Events.Include(e => e.Organiser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            if (!EventTypes.TryNormalise(eventType, out var normalised))
            {
                return Fail(StatusCodes.Status400BadRequest, "eventType must be Run, Walk or Cycle.");
            }
            query = query.Where(e => e.EventType == normalised);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    /// <summary>Gets one event by its ID.</summary>
    /// <response code="200">The event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("{id:int}")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var raceEvent = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);
        return raceEvent is null
            ? Fail(StatusCodes.Status404NotFound, "Event not found.")
            : Ok(raceEvent.ToResponse());
    }

    /// <summary>Creates a new event. Organisers only.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in but not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] EventRequest request)
    {
        if (!EventTypes.TryNormalise(request.EventType, out var eventType))
        {
            return Fail(StatusCodes.Status400BadRequest, "eventType must be Run, Walk or Cycle.");
        }
        if (request.EventDate.Date < DateTime.UtcNow.Date)
        {
            return Fail(StatusCodes.Status400BadRequest, "The event date cannot be in the past.");
        }

        var raceEvent = new RaceEvent
        {
            EventName = request.Name.Trim(),
            EventDetails = request.Description?.Trim(),
            EventDate = request.EventDate.Date,
            Venue = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = eventType,
            OrganiserId = CurrentUserId,
            DateCreated = DateTime.UtcNow
        };

        _db.Events.Add(raceEvent);
        await _db.SaveChangesAsync();
        await _db.Entry(raceEvent).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = raceEvent.EventId }, raceEvent.ToResponse());
    }

    /// <summary>Updates an event. Only the Organiser who created it can do this.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] EventRequest request)
    {
        var raceEvent = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (raceEvent is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Event not found.");
        }
        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Fail(StatusCodes.Status403Forbidden, "You can only change events that you created.");
        }
        if (!EventTypes.TryNormalise(request.EventType, out var eventType))
        {
            return Fail(StatusCodes.Status400BadRequest, "eventType must be Run, Walk or Cycle.");
        }

        raceEvent.EventName = request.Name.Trim();
        raceEvent.EventDetails = request.Description?.Trim();
        raceEvent.EventDate = request.EventDate.Date;
        raceEvent.Venue = request.Location.Trim();
        raceEvent.DistanceKm = request.DistanceKm;
        raceEvent.EventType = eventType;

        await _db.SaveChangesAsync();
        return Ok(raceEvent.ToResponse());
    }

    /// <summary>Deletes an event. Only the Organiser who created it can do this.</summary>
    /// <remarks>An event that already has categories or enrolments cannot be deleted.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    /// <response code="409">The event still has categories or enrolments.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var raceEvent = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (raceEvent is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Event not found.");
        }
        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Fail(StatusCodes.Status403Forbidden, "You can only delete events that you created.");
        }
        if (await _db.Categories.AnyAsync(c => c.EventId == id)
            || await _db.Enrolments.AnyAsync(en => en.EventId == id))
        {
            return Fail(StatusCodes.Status409Conflict, "This event has categories or enrolments and cannot be deleted.");
        }

        _db.Events.Remove(raceEvent);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}