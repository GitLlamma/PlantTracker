using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Auth;

namespace PlantTracker.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _zipCode = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    public RegisterViewModel(AuthService auth)
    {
        _auth = auth;
        Title = "Create Account";
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(DisplayName) || string.IsNullOrWhiteSpace(ZipCode))
        {
            ErrorMessage = "Please fill in all fields.";
            HasError = true;
            return;
        }

        IsBusy = true;
        HasError = false;

        try
        {
            var (success, error) = await _auth.RegisterAsync(new RegisterRequestDto
            {
                Email = Email,
                Password = Password,
                DisplayName = DisplayName,
                ZipCode = ZipCode
            });

            if (success)
                await Shell.Current.GoToAsync("//Search");
            else
            {
                ErrorMessage = error ?? "Registration failed.";
                HasError = true;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToLoginAsync() =>
        await Shell.Current.GoToAsync("//Login");
}

