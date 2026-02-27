using System.ComponentModel.DataAnnotations;

namespace PlantTracker.Shared.DTOs.Garden;

/// <summary>
/// Payload for adding a plant to the user's garden.
/// </summary>
public class AddUserPlantDto
{
    [Required]
    public int PlantId { get; set; }

    // Denormalized display fields passed in from the MAUI app so we don't need
    // to call the Perenual API again just to store a name and thumbnail.
    [Required]
    public string CommonName { get; set; } = string.Empty;
    public string ScientificName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }

    public string? Notes { get; set; }
    public bool WateringReminderEnabled { get; set; }
    public int? WateringFrequencyDays { get; set; }

    // Custom plant care attributes (not needed for Perenual plants, stored for display)
    public string? Watering { get; set; }
    public string? Sunlight { get; set; }
    public string? Cycle { get; set; }
    public string? CareLevel { get; set; }
}

