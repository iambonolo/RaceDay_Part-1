using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class CategoryRequest : IValidatableObject
{
    /// <example>Under 18</example>
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <example>10</example>
    [Range(0, 120)]
    public int MinAge { get; set; }

    /// <example>17</example>
    [Range(0, 120)]
    public int MaxAge { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxAge < MinAge)
        {
            yield return new ValidationResult("maxAge must be greater than or equal to minAge.", new[] { nameof(MaxAge) });
        }
    }
}

public record CategoryResponse(int CategoryId, int EventId, string Name, int MinAge, int MaxAge);

public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.MinAge, c.MaxAge);
}