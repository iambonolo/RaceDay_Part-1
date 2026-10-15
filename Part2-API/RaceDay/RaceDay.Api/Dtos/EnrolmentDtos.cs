using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EnrolRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class ResultRequest
{
    /// <summary>Finish time in hh:mm:ss format.</summary>
    /// <example>00:52:30</example>
    [Required]
    public string FinishTime { get; set; } = string.Empty;

    /// <example>1</example>
    [Range(1, 100000)]
    public int FinishPosition { get; set; }
}

public record EnrolmentResponse(
    int EnrolmentId, int EventId, string? EventName, DateTime? EventDate,
    int CategoryId, string? CategoryName,
    int ParticipantId, string? ParticipantName,
    DateTime DateEnrolled, string Status);

public record ResultResponse(
    int ResultId, int EnrolmentId, string FinishTime, int FinishPosition, DateTime DateCaptured,
    int EventId, string? EventName, string? CategoryName, string? ParticipantName);

public static class EnrolmentMappings
{
    public static EnrolmentResponse ToResponse(this Enrolment e) =>
        new(e.EnrolmentId, e.EventId, e.Event?.EventName, e.Event?.EventDate,
            e.CategoryId, e.Category?.CategoryName,
            e.ParticipantId, e.Participant?.FullName,
            e.DateEnrolled, e.EnrolmentStatus);

    public static ResultResponse ToResponse(this Result r) =>
        new(r.ResultId, r.EnrolmentId, r.FinishTime.ToString(@"hh\:mm\:ss"), r.FinishPosition, r.DateCaptured,
            r.Enrolment?.EventId ?? 0, r.Enrolment?.Event?.EventName,
            r.Enrolment?.Category?.CategoryName, r.Enrolment?.Participant?.FullName);
}