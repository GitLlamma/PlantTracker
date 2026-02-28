namespace PlantTracker.Shared.DTOs.Garden;

/// <summary>
/// Payload for updating notes or reminder settings on a saved plant.
/// </summary>
public class UpdateUserPlantDto
{
    // Editable for custom plants; ignored for Perenual plants on the API side
    public string? CommonName { get; set; }
    public string? ScientificName { get; set; }

    public string? Notes { get; set; }
    public string? Nickname { get; set; }
    public bool WateringReminderEnabled { get; set; }
    public int? WateringFrequencyDays { get; set; }
    public DateTime? LastWateredAt { get; set; }

    // Care attributes — stored for custom plants, ignored for Perenual plants
    public string? Watering { get; set; }
    public string? Sunlight { get; set; }
    public string? Cycle { get; set; }
    public string? CareLevel { get; set; }

    // Cover photo — updatable for all plants
    public string? ThumbnailUrl { get; set; }
}
