namespace PlantTracker.Messages;

/// <summary>
/// Sent when the user sets a new cover photo for a garden plant,
/// so MyGardenViewModel can refresh the thumbnail without tight coupling.
/// </summary>
public record PlantCoverPhotoChangedMessage(int UserPlantId, string ImageData);

