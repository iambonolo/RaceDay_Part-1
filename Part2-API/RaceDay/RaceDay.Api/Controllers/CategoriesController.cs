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
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists the age categories of an event. Any logged-in user can use this.</summary>
    /// <response code="200">The categories.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return Fail(StatusCodes.Status404NotFound, "Event not found.");
        }

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.MinAge)
            .ToListAsync();
        return Ok(categories.Select(c => c.ToResponse()));
    }

    /// <summary>Adds a category to an event. Only the Organiser who owns the event can do this.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    /// <response code="409">A category with that name already exists for this event.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest request)
    {
        var raceEvent = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Event not found.");
        }
        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Fail(StatusCodes.Status403Forbidden, "You can only add categories to events that you created.");
        }

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name))
        {
            return Fail(StatusCodes.Status409Conflict, "This event already has a category with that name.");
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = name,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", category.ToResponse());
    }

    /// <summary>Updates a category. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Category not found.");
        }
        if (category.Event!.OrganiserId != CurrentUserId)
        {
            return Fail(StatusCodes.Status403Forbidden, "You can only change categories of events that you created.");
        }

        category.CategoryName = request.Name.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        await _db.SaveChangesAsync();

        return Ok(category.ToResponse());
    }

    /// <summary>Deletes a category. Only the Organiser who owns the event can do this.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">No category with that ID.</response>
    /// <response code="409">Participants are already enrolled in this category.</response>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return Fail(StatusCodes.Status404NotFound, "Category not found.");
        }
        if (category.Event!.OrganiserId != CurrentUserId)
        {
            return Fail(StatusCodes.Status403Forbidden, "You can only delete categories of events that you created.");
        }
        if (await _db.Enrolments.AnyAsync(en => en.CategoryId == id))
        {
            return Fail(StatusCodes.Status409Conflict, "Participants are enrolled in this category, so it cannot be deleted.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}