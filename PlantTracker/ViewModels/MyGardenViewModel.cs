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

        // First load — show spinner and wait for network
        if (!hasCachedData)
        {
            IsBusy = true;
            try
            {
                var plants = await _garden.GetGardenAsync();
                ApplyDiff(plants);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
            return;
        }

        // Cache hit — apply instantly with no spinner, then refresh in background
        ApplyDiff(_garden.GetCachedPlants());

        _ = Task.Run(async () =>
        {
            try
            {
                await _garden.RefreshCacheAsync();
                var refreshed = _garden.GetCachedPlants();
                await MainThread.InvokeOnMainThreadAsync(() => ApplyDiff(refreshed));
            }
            catch { /* silent — cache still valid */ }
        });
    }

    /// <summary>
    /// Applies a new plant list to the ObservableCollection without clearing it.
    /// Only adds, removes, and updates items that actually changed — avoiding a full
    /// CollectionView re-layout on every navigation to the tab.
    /// </summary>
    private void ApplyDiff(List<UserPlantDto> incoming)
    {
        // Remove plants no longer in the list
        var incomingIds = incoming.Select(p => p.Id).ToHashSet();
        for (var i = Plants.Count - 1; i >= 0; i--)
        {
            if (!incomingIds.Contains(Plants[i].Plant.Id))
                Plants.RemoveAt(i);
        }

        // Add or update
        for (var i = 0; i < incoming.Count; i++)
        {
            var dto = incoming[i];
            var existing = Plants.FirstOrDefault(p => p.Plant.Id == dto.Id);

            if (existing is null)
            {
                // New plant — insert at correct position
                Plants.Insert(Math.Min(i, Plants.Count), new GardenPlantItem(dto));
            }
            else if (existing.Plant != dto)
            {
                // Updated plant — reassign to trigger bindings
                existing.Plant = dto;
            }
        }

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
                Nickname                = plant.Nickname,
                Notes                   = plant.Notes,
                CommonName              = plant.CommonName,
                ScientificName          = plant.ScientificName,
                Watering                = plant.Watering,
                Sunlight                = plant.Sunlight,
                Cycle                   = plant.Cycle,
                CareLevel               = plant.CareLevel,
                ThumbnailUrl            = plant.ThumbnailUrl,
                WateringReminderEnabled = false,
                WateringFrequencyDays   = plant.WateringFrequencyDays,
                LastWateredAt           = plant.LastWateredAt
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
                Nickname                = plant.Nickname,
                Notes                   = plant.Notes,
                CommonName              = plant.CommonName,
                ScientificName          = plant.ScientificName,
                Watering                = plant.Watering,
                Sunlight                = plant.Sunlight,
                Cycle                   = plant.Cycle,
                CareLevel               = plant.CareLevel,
                ThumbnailUrl            = plant.ThumbnailUrl,
                WateringReminderEnabled = true,
                WateringFrequencyDays   = days,
                LastWateredAt           = plant.LastWateredAt
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
