namespace PlantTracker.Api.Models;

/// <summary>
/// Represents a plant saved to a user's garden. Stored in the API database.
/// </summary>
public class UserPlant
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Perenual plant ID — used to look up cached/live plant details
    public int PlantId { get; set; }

    // Denormalized for fast list rendering without extra API calls
    public string CommonName { get; set; } = string.Empty;
    public string ScientificName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }

    public string? Notes { get; set; }
    public bool WateringReminderEnabled { get; set; }
    public int? WateringFrequencyDays { get; set; }
    public DateTime? LastWateredAt { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

