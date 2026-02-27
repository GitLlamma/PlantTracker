namespace PlantTracker.Shared.DTOs.Plants;

/// <summary>
/// Full plant details returned from the detail endpoint / Perenual API.
/// </summary>
public class PlantDetailDto
{
    public int Id { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public string ScientificName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }

    // Care requirements
    public string? Watering { get; set; }           // e.g. "Frequent", "Average", "Minimum", "None"
    public int? WateringFrequencyDays { get; set; } // derived interval used for reminder scheduling
    public List<string> Sunlight { get; set; } = [];  // e.g. ["full sun", "part shade"]
    public string? Cycle { get; set; }              // e.g. "Perennial", "Annual", "Biennial"
    public string? CareLevel { get; set; }          // e.g. "Low", "Medium", "High"

    // Planting / zone info
    public int? HardinessZoneMin { get; set; }
    public int? HardinessZoneMax { get; set; }

    // Dimensions
    public string? MaxHeightFt { get; set; }
    public string? MaxSpreadFt { get; set; }
}

