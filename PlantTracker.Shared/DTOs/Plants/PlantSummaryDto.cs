namespace PlantTracker.Shared.DTOs.Plants;

/// <summary>
/// A single result item returned from a plant search query.
/// </summary>
public class PlantSummaryDto
{
    public int Id { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public string ScientificName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Cycle { get; set; }         // e.g. "Perennial", "Annual"
    public string? Watering { get; set; }      // e.g. "Frequent", "Average", "Minimum"
}

