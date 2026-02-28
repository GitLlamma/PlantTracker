namespace PlantTracker.Shared.DTOs.Garden;

public class PlantPhotoDto
{
    public int Id { get; set; }
    public int UserPlantId { get; set; }
    public string ImageData { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime TakenAt { get; set; }
}

