using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    /// <example>Soweto 10K Fun Run</example>
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <example>A scenic 10km road run through Soweto.</example>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <example>2026-12-05</example>
    public DateTime EventDate { get; set; }

    /// <example>Orlando Stadium, Soweto</example>
    [Required, StringLength(100)]
    public string Location { get; set; } = string.Empty;

    /// <example>10</example>
    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>Must be "Run", "Walk" or "Cycle".</summary>
    /// <example>Run</example>
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public record EventResponse(
    int EventId,
    string Name,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    int OrganiserId,
    string? OrganiserName,
    DateTime DateCreated);

public static class EventMappings
{
    public static EventResponse ToResponse(this RaceEvent e) =>
        new(e.EventId, e.EventName, e.EventDetails, e.EventDate, e.Venue,
            e.DistanceKm, e.EventType, e.OrganiserId, e.Organiser?.FullName, e.DateCreated);
}