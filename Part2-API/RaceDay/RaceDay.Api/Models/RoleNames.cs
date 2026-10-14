namespace RaceDay.Api.Models;

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static bool TryNormalise(string? value, out string roleName)
    {
        var text = value?.Trim();
        if (string.Equals(text, Organiser, StringComparison.OrdinalIgnoreCase)) { roleName = Organiser; return true; }
        if (string.Equals(text, Participant, StringComparison.OrdinalIgnoreCase)) { roleName = Participant; return true; }
        roleName = string.Empty;
        return false;
    }
}

public static class EventTypes
{
    public static readonly string[] All = { "Run", "Walk", "Cycle" };

    public static bool TryNormalise(string? value, out string eventType)
    {
        var match = All.FirstOrDefault(t => string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        eventType = match ?? string.Empty;
        return match != null;
    }
}