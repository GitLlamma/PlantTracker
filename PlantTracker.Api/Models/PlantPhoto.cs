namespace PlantTracker.Api.Models;

/// <summary>
/// A user-uploaded photo of a plant in their garden.
/// The image is stored as a base64 data URI so no external blob storage is needed.
/// </summary>
public class PlantPhoto
{
    public int Id { get; set; }
    public int UserPlantId { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Full base64 data URI, e.g. "data:image/jpeg;base64,/9j/4AAQ..."</summary>
    public string ImageData { get; set; } = string.Empty;

    public string? Caption { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;
}

