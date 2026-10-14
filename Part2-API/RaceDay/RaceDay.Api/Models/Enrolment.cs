namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }
    public AppUser? Participant { get; set; }

    public int EventId { get; set; }
    public RaceEvent? Event { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime DateEnrolled { get; set; } = DateTime.UtcNow;
    public string EnrolmentStatus { get; set; } = "Confirmed";

    public Result? Result { get; set; }
}