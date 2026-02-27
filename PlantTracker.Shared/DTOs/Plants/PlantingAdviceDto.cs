namespace PlantTracker.Shared.DTOs.Plants;

/// <summary>
/// The result of comparing a user's hardiness zone against a plant's ideal zone range.
/// </summary>
public class PlantingAdviceDto
{
    public int UserZone { get; set; }
    public int PlantZoneMin { get; set; }
    public int PlantZoneMax { get; set; }
    public PlantingRecommendation Recommendation { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum PlantingRecommendation
{
    PlantNowOutdoors,
    StartIndoors,
    Wait,
    ZoneNotCompatible
}

