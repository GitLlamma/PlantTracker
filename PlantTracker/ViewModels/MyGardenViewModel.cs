using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PlantTracker.Messages;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.ViewModels;

public partial class MyGardenViewModel : BaseViewModel, IRecipient<GardenPlantAddedMessage>
{
    private readonly GardenService _garden;

    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<UserPlantDto> Plants { get; } = [];

    public MyGardenViewModel(GardenService garden)
    {
        _garden = garden;
        Title = "My Garden";
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(GardenPlantAddedMessage message)
    {
        // Refresh garden list when a plant is added from the detail page
        MainThread.BeginInvokeOnMainThread(() =>
            LoadGardenCommand.ExecuteAsync(null));
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
                Plants.Add(p);

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
    private async Task GoToPlantDetailAsync(UserPlantDto plant)
    {
        var summary = new PlantSummaryDto
        {
            Id = plant.PlantId,
            CommonName = plant.CommonName,
            ScientificName = plant.ScientificName,
            ThumbnailUrl = plant.ThumbnailUrl
        };

        await Shell.Current.GoToAsync("PlantDetail", new Dictionary<string, object>
        {
            { "PlantId", plant.PlantId },
            { "PlantSummary", summary }
        });
    }

    [RelayCommand]
    private async Task EditReminderAsync(UserPlantDto plant)
    {
        // Toggle reminder on/off with frequency prompt
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
                ReplaceInList(plant, updated);
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
                ReplaceInList(plant, updated);
        }
    }

    private void ReplaceInList(UserPlantDto old, UserPlantDto updated)
    {
        var index = Plants.IndexOf(old);
        if (index >= 0) Plants[index] = updated;
    }
    [RelayCommand]
    private async Task MarkWateredAsync(UserPlantDto plant)
    {
        var (success, updated) = await _garden.MarkWateredAsync(plant.Id);
        if (success && updated is not null)
            ReplaceInList(plant, updated);
    }

    [RelayCommand]
    private async Task RemovePlantAsync(UserPlantDto plant)
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Remove Plant",
            $"Remove {plant.CommonName} from your garden?",
            "Remove", "Cancel");

        if (!confirm) return;

        var success = await _garden.RemovePlantAsync(plant.Id);
        if (success)
        {
            Plants.Remove(plant);
            IsEmpty = Plants.Count == 0;
        }
    }
}

