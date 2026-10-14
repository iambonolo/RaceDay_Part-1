namespace RaceDay.Api.Models;

// Maps to the Events table.
public class RaceEvent
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? EventDetails { get; set; }
    public DateTime EventDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;   // Run, Walk or Cycle

    public int OrganiserId { get; set; }
    public AppUser? Organiser { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}