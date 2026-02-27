using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PlantTracker.Messages;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.ViewModels;

/// <summary>
/// Backs the Reminders tab. Shows only garden plants that have watering reminders
/// enabled, sorted by urgency (most overdue first).
/// </summary>
public partial class RemindersViewModel : BaseViewModel, IRecipient<GardenPlantAddedMessage>
{
    private readonly GardenService _garden;

    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<ReminderItem> Reminders { get; } = [];

    public RemindersViewModel(GardenService garden)
    {
        _garden = garden;
        Title = "Reminders";
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(GardenPlantAddedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            LoadRemindersCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    public async Task LoadRemindersAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var plants = await _garden.GetGardenAsync();

            var items = plants
                .Where(p => p.WateringReminderEnabled && p.WateringFrequencyDays.HasValue)
                .Select(p => new ReminderItem(p))
                .OrderBy(r => r.DaysUntilDue)   // overdue (negative) first
                .ToList();

            Reminders.Clear();
            foreach (var item in items)
                Reminders.Add(item);

            IsEmpty = Reminders.Count == 0;
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
    private async Task MarkWateredAsync(ReminderItem item)
    {
        var (success, updated) = await _garden.MarkWateredAsync(item.Plant.Id);
        if (!success || updated is null) return;

        var index = Reminders.IndexOf(item);
        if (index >= 0)
            Reminders[index] = new ReminderItem(updated);
    }

    [RelayCommand]
    private async Task DisableReminderAsync(ReminderItem item)
    {
        var dto = new UpdateUserPlantDto
        {
            Notes = item.Plant.Notes,
            WateringReminderEnabled = false,
            WateringFrequencyDays = item.Plant.WateringFrequencyDays,
            LastWateredAt = item.Plant.LastWateredAt
        };

        var (success, _, _) = await _garden.UpdatePlantAsync(item.Plant.Id, dto);
        if (success)
            Reminders.Remove(item);

        IsEmpty = Reminders.Count == 0;
    }
}

/// <summary>
/// Wraps a UserPlantDto with computed urgency info for display in the Reminders list.
/// </summary>
public class ReminderItem(UserPlantDto plant) : ObservableObject
{
    public UserPlantDto Plant { get; } = plant;

    /// <summary>Days until next watering is due. Negative means overdue.</summary>
    public int DaysUntilDue
    {
        get
        {
            if (!Plant.WateringFrequencyDays.HasValue) return int.MaxValue;

            var baseline = Plant.LastWateredAt ?? Plant.AddedAt;
            var dueDate = baseline.AddDays(Plant.WateringFrequencyDays.Value);
            return (int)(dueDate.Date - DateTime.UtcNow.Date).TotalDays;
        }
    }

    public bool IsOverdue => DaysUntilDue < 0;

    public string StatusText => DaysUntilDue switch
    {
        < 0  => $"Overdue by {-DaysUntilDue} day{(-DaysUntilDue == 1 ? "" : "s")}!",
        0    => "Due today!",
        1    => "Due tomorrow",
        _    => $"Due in {DaysUntilDue} days"
    };

    public Color StatusBackgroundColor => IsOverdue ? Color.FromArgb("#FFEEEE") : Color.FromArgb("#EEF8EE");
    public Color StatusTextColor       => IsOverdue ? Color.FromArgb("#CC2222") : Color.FromArgb("#226622");

    public string FrequencyText =>
        Plant.WateringFrequencyDays.HasValue
            ? $"Every {Plant.WateringFrequencyDays} day{(Plant.WateringFrequencyDays == 1 ? "" : "s")}"
            : string.Empty;
}

