using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PlantTracker.Messages;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.ViewModels;

/// <summary>
/// Wraps a UserPlantDto with per-item busy state so that watering one plant
/// only disables that card's button, not every Water button in the list.
/// </summary>
public partial class GardenPlantItem(UserPlantDto plant) : ObservableObject
{
    [ObservableProperty] private UserPlantDto _plant = plant;
    [ObservableProperty] private bool _isWatering;
}

public partial class MyGardenViewModel : BaseViewModel, IRecipient<GardenPlantAddedMessage>, IRecipient<PlantCoverPhotoChangedMessage>
{
    private readonly GardenService _garden;
    private readonly EditPlantViewModel _editVm;

    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<GardenPlantItem> Plants { get; } = [];

    public MyGardenViewModel(GardenService garden, EditPlantViewModel editVm)
    {
        _garden = garden;
        _editVm = editVm;
        Title = "My Garden";
        WeakReferenceMessenger.Default.Register<GardenPlantAddedMessage>(this);
        WeakReferenceMessenger.Default.Register<PlantCoverPhotoChangedMessage>(this);
    }

    public void Receive(GardenPlantAddedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            LoadGardenCommand.ExecuteAsync(null));
    }

    public void Receive(PlantCoverPhotoChangedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Plants.FirstOrDefault(p => p.Plant.Id == message.UserPlantId);
            if (item is null) return;

            // Mutate ThumbnailUrl then re-assign Plant to trigger the ObservableProperty notification
            item.Plant.ThumbnailUrl = message.ImageData;
            item.Plant = item.Plant;
        });
    }

    [RelayCommand]
    public async Task LoadGardenAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var plants = await _garden.GetGardenAsync();
            Plants.Clear();
            foreach (var p in plants)
                Plants.Add(new GardenPlantItem(p));

            IsEmpty = Plants.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static async Task AddCustomPlantAsync() =>
        await Shell.Current.GoToAsync("AddCustomPlant");

    [RelayCommand]
    private async Task EditPlantAsync(GardenPlantItem item)
    {
        _editVm.InitForEdit(item.Plant);
        await Shell.Current.GoToAsync("EditPlant");
    }

    [RelayCommand]
    private async Task GoToPlantDetailAsync(GardenPlantItem item)
    {
        if (item.Plant.PlantId == 0)
        {
            await Shell.Current.GoToAsync("PlantDetail", new Dictionary<string, object>
            {
                { "UserPlant", item.Plant }
            });
        }
        else
        {
            var summary = new PlantSummaryDto
            {
                Id = item.Plant.PlantId,
                CommonName = item.Plant.CommonName,
                ScientificName = item.Plant.ScientificName,
                ThumbnailUrl = item.Plant.ThumbnailUrl
            };

            await Shell.Current.GoToAsync("PlantDetail", new Dictionary<string, object>
            {
                { "PlantId", item.Plant.PlantId },
                { "PlantSummary", summary },
                { "UserPlant", item.Plant }
            });
        }
    }

    [RelayCommand]
    private async Task EditReminderAsync(GardenPlantItem item)
    {
        var plant = item.Plant;

        if (plant.WateringReminderEnabled)
        {
            var confirm = await Shell.Current.DisplayAlertAsync(
                "Watering Reminder",
                $"Disable reminder for {plant.CommonName}?",
                "Disable", "Cancel");
            if (!confirm) return;

            var dto = new UpdateUserPlantDto
            {
                Notes = plant.Notes,
                WateringReminderEnabled = false,
                WateringFrequencyDays = plant.WateringFrequencyDays,
                LastWateredAt = plant.LastWateredAt
            };
            var (success, updated, _) = await _garden.UpdatePlantAsync(plant.Id, dto);
            if (success && updated is not null)
                item.Plant = updated;
        }
        else
        {
            var freqStr = await Shell.Current.DisplayPromptAsync(
                "Watering Reminder",
                $"How often does {plant.CommonName} need watering?\nEnter number of days:",
                "Enable",
                "Cancel",
                placeholder: "e.g. 7",
                keyboard: Keyboard.Numeric,
                initialValue: plant.WateringFrequencyDays?.ToString() ?? "7");

            if (freqStr is null) return;
            if (!int.TryParse(freqStr, out var days) || days < 1)
            {
                await Shell.Current.DisplayAlertAsync("Invalid", "Please enter a valid number of days (1 or more).", "OK");
                return;
            }

            var dto = new UpdateUserPlantDto
            {
                Notes = plant.Notes,
                WateringReminderEnabled = true,
                WateringFrequencyDays = days,
                LastWateredAt = plant.LastWateredAt
            };
            var (success, updated, _) = await _garden.UpdatePlantAsync(plant.Id, dto);
            if (success && updated is not null)
                item.Plant = updated;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task MarkWateredAsync(GardenPlantItem item)
    {
        if (item.IsWatering) return;
        item.IsWatering = true;
        try
        {
            var (success, updated) = await _garden.MarkWateredAsync(item.Plant.Id);
            if (success && updated is not null)
                item.Plant = updated;
        }
        finally
        {
            item.IsWatering = false;
        }
    }

    [RelayCommand]
    private async Task RemovePlantAsync(GardenPlantItem item)
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Remove Plant",
            $"Remove {item.Plant.CommonName} from your garden?",
            "Remove", "Cancel");

        if (!confirm) return;

        var success = await _garden.RemovePlantAsync(item.Plant.Id);
        if (success)
        {
            Plants.Remove(item);
            IsEmpty = Plants.Count == 0;
        }
    }
}
