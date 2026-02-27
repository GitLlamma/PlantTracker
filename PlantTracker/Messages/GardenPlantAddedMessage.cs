namespace PlantTracker.Messages;

/// <summary>
/// Sent via WeakReferenceMessenger when a plant is successfully added to the garden,
/// so MyGardenViewModel can refresh its list without tight coupling.
/// </summary>
public record GardenPlantAddedMessage;

