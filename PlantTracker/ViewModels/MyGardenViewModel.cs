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
public partial class GardenPlantItem : ObservableObject
{
    [ObservableProperty] private UserPlantDto _plant;
    [ObservableProperty] private bool _isWatering;
    [ObservableProperty] private bool _reminderEnabled;

    public GardenPlantItem(UserPlantDto plant)
    {
        _plant = plant;
        _reminderEnabled = plant.WateringReminderEnabled;
    }

    partial void OnPlantChanged(UserPlantDto value)
    {
        ReminderEnabled = value.WateringReminderEnabled;
    }
}

public partial class MyGardenViewModel : BaseViewModel, IRecipient<GardenPlantAddedMessage>, IRecipient<PlantCoverPhotoChangedMessage>
{
    private readonly GardenService _garden;
    private readonly EditPlantViewModel _editVm;
    private readonly NotificationService _notifications;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FooterHeight))]
    private bool _isEmpty;

    public double FooterHeight => IsEmpty ? 0 : 120;

    // Set by the code-behind once it knows whether the list overflows the screen.
    [ObservableProperty] private bool _footerVisible;

    public ObservableCollection<GardenPlantItem> Plants { get; } = [];

    public MyGardenViewModel(GardenService garden, EditPlantViewModel editVm, NotificationService notifications)
    {
        _garden = garden;
        _editVm = editVm;
        _notifications = notifications;
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

        var hasCachedData = Plants.Count > 0;

        // Only show the loading spinner on the very first load (no data visible yet)
        if (!hasCachedData)
            IsBusy = true;

        try
        {
            // GetGardenAsync returns cache immediately if available, then refreshes in background.
            // On first load it waits for the network. Either way we apply whatever comes back.
            var plants = await _garden.GetGardenAsync();
            ApplyPlants(plants);

            // If cache was returned instantly, the background refresh is already running.
            // Subscribe to get the refreshed data once it lands and silently update the UI.
            if (hasCachedData)
            {
                _ = Task.Run(async () =>
                {
                    // Small delay to let RefreshCacheAsync finish
                    await Task.Delay(100);
                    var refreshed = await _garden.GetGardenAsync();
                    await MainThread.InvokeOnMainThreadAsync(() => ApplyPlants(refreshed));
                });
            }
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

    private void ApplyPlants(List<UserPlantDto> plants)
    {
        Plants.Clear();
        foreach (var p in plants)
            Plants.Add(new GardenPlantItem(p));
        IsEmpty = Plants.Count == 0;
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
                $"Disable reminder for {plant.Nickname ?? plant.CommonName}?",
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
            {
                item.Plant = updated;
                _notifications.Cancel(plant.Id);
            }
        }
        else
        {
            var defaultTime = _notifications.GetDefaultReminderTime();
            var timeStr = DateTime.Today.Add(defaultTime).ToString("h:mm tt");

            var freqStr = await Shell.Current.DisplayPromptAsync(
                "Watering Reminder",
                $"How often does {plant.Nickname ?? plant.CommonName} need watering?\n\nReminder will fire daily at {timeStr}\n(Change the default time in Settings)\n\nEnter number of days:",
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
            {
                item.Plant = updated;
                await _notifications.ScheduleAsync(plant.Id, plant.Nickname ?? plant.CommonName, days, defaultTime);
            }
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
