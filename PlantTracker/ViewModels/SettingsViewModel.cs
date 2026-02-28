using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantTracker.Services;

namespace PlantTracker.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly PlantService _plants;
    private readonly NotificationService _notifications;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _zipCode = string.Empty;
    [ObservableProperty] private TimeSpan _reminderTime;

    private bool _initialized;

    public SettingsViewModel(AuthService auth, PlantService plants, NotificationService notifications)
    {
        _auth = auth;
        _plants = plants;
        _notifications = notifications;
        Title = "Settings";
        ReminderTime = _notifications.GetDefaultReminderTime();
        _initialized = true;
    }

    partial void OnReminderTimeChanged(TimeSpan value)
    {
        if (!_initialized) return;
        _notifications.SaveDefaultReminderTime(value);
    }

    [RelayCommand]
    public async Task LoadUserAsync()
    {
        var user = await _auth.GetCurrentUserAsync();
        if (user is null) return;

        DisplayName = user.DisplayName;
        Email = user.Email;
        ZipCode = user.ZipCode;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Sign Out", "Are you sure you want to sign out?", "Sign Out", "Cancel");

        if (!confirm) return;

        _plants.ClearCache();
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//Login");
    }
}

