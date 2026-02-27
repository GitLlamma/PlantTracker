using System.ComponentModel.DataAnnotations;

namespace PlantTracker.Shared.DTOs.Garden;

/// <summary>
/// Payload for adding a plant to the user's garden.
/// </summary>
public class AddUserPlantDto
{
    [Required]
    public int PlantId { get; set; }

    public string? Notes { get; set; }
    public bool WateringReminderEnabled { get; set; }
    public int? WateringFrequencyDays { get; set; }
}

