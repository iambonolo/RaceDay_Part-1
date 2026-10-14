using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    /// <example>Thandi Nkosi</example>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>thandi@example.com</example>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <example>StrongPass1!</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    /// <example>Participant</example>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>Only needed when the server is configured to require an organiser code.</summary>
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    /// <example>refilwe.modise@raceday.co.za</example>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}

public record UserProfileResponse(int UserId, string FullName, string Email, string Role, DateTime DateJoined);

public record MessageResponse(string Message);

public static class UserMappings
{
    public static UserProfileResponse ToProfile(this AppUser user) =>
        new(user.UserId, user.FullName, user.Email, user.Role?.RoleName ?? string.Empty, user.DateJoined);
}