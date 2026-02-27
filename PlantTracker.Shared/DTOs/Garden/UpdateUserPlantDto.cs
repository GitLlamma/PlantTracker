namespace PlantTracker.Shared.DTOs.Garden;

/// <summary>
/// Payload for updating notes or reminder settings on a saved plant.
/// </summary>
public class UpdateUserPlantDto
{
    public string? Notes { get; set; }
    public bool WateringReminderEnabled { get; set; }
    public int? WateringFrequencyDays { get; set; }
    public DateTime? LastWateredAt { get; set; }
}

