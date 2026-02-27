using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantTracker.Services;

namespace PlantTracker.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _zipCode = string.Empty;

    public SettingsViewModel(AuthService auth)
    {
        _auth = auth;
        Title = "Settings";
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
        var confirm = await Shell.Current.DisplayAlert(
            "Sign Out", "Are you sure you want to sign out?", "Sign Out", "Cancel");

        if (!confirm) return;

        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//Login");
    }
}

