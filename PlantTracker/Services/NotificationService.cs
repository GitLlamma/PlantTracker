using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace PlantTracker.Services;

/// <summary>
/// Wraps Plugin.LocalNotification to schedule / cancel per-plant watering reminders.
/// Each plant gets a notification whose ID is its UserPlant.Id (guaranteed unique per user).
/// The notification fires daily at the user-chosen time, starting from the next occurrence
/// of that time on or after today.
/// </summary>
public class NotificationService
{
    // Preferences key for the global default reminder time.
    public const string PrefHour   = "reminder_hour";
    public const string PrefMinute = "reminder_minute";

    // Sensible default: 9:00 AM
    public const int DefaultHour   = 9;
    public const int DefaultMinute = 0;

    /// <summary>Returns the user's saved default reminder time.</summary>
    public TimeSpan GetDefaultReminderTime()
    {
        var h = Preferences.Get(PrefHour,   DefaultHour);
        var m = Preferences.Get(PrefMinute, DefaultMinute);
        return new TimeSpan(h, m, 0);
    }

    /// <summary>Saves the global default reminder time.</summary>
    public void SaveDefaultReminderTime(TimeSpan time)
    {
        Preferences.Set(PrefHour,   time.Hours);
        Preferences.Set(PrefMinute, time.Minutes);
    }

    /// <summary>
    /// Requests the OS notification permission.  Returns true if granted.
    /// On Android 13+ this shows the system permission dialog the first time.
    /// </summary>
    public async Task<bool> RequestPermissionAsync()
    {
        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    /// <summary>
    /// Schedules a repeating daily notification for the given plant.
    /// Call this whenever a reminder is enabled or its settings change.
    /// </summary>
    public async Task ScheduleAsync(int userPlantId, string plantName, int frequencyDays, TimeSpan notifyAt)
    {
        // Cancel any existing notification for this plant first.
        Cancel(userPlantId);

        var granted = await RequestPermissionAsync();
        if (!granted) return;

        // Calculate the next fire time: today at notifyAt, or tomorrow if that's already past.
        var now    = DateTime.Now;
        var today  = now.Date.Add(notifyAt);
        var notify = today > now ? today : today.AddDays(1);

        var request = new NotificationRequest
        {
            NotificationId = userPlantId,
            Title          = "🪴 Time to water!",
            Description    = $"{plantName} needs watering today.",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime   = notify,
                RepeatType   = NotificationRepeat.Daily,
            },
            Android = new AndroidOptions
            {
                // Use a dedicated channel so users can silence plant reminders independently.
                ChannelId = "plant_watering",
            }
        };

        await LocalNotificationCenter.Current.Show(request);
    }

    /// <summary>Cancels any scheduled notification for the given plant.</summary>
    public void Cancel(int userPlantId)
    {
        LocalNotificationCenter.Current.Cancel(userPlantId);
    }
}


