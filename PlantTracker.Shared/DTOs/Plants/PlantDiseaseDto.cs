namespace PlantTracker.Shared.DTOs.Plants;

public class PlantDiseaseDto
{
    public int Id { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Solution { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<string> HostPlants { get; set; } = [];
}

