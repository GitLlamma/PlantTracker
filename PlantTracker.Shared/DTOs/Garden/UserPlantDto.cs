namespace PlantTracker.Shared.DTOs.Garden;

/// <summary>
/// Represents a plant saved to the user's garden (sent to and from the API).
/// </summary>
public class UserPlantDto
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public string ScientificName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Notes { get; set; }
    public bool WateringReminderEnabled { get; set; }
    public int? WateringFrequencyDays { get; set; }
    public DateTime? LastWateredAt { get; set; }
    public DateTime AddedAt { get; set; }

    // Custom plant care attributes
    public string? Watering { get; set; }
    public string? Sunlight { get; set; }
    public string? Cycle { get; set; }
    public string? CareLevel { get; set; }
}

