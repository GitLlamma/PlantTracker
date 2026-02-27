using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Auth;

namespace PlantTracker.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    public LoginViewModel(AuthService auth)
    {
        _auth = auth;
        Title = "Sign In";
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your email and password.";
            HasError = true;
            return;
        }

        IsBusy = true;
        HasError = false;

        try
        {
            var (success, error) = await _auth.LoginAsync(new LoginRequestDto
            {
                Email = Email,
                Password = Password
            });

            if (success)
            {
                await Shell.Current.GoToAsync("//Search");
            }
            else
            {
                ErrorMessage = error ?? "Login failed.";
                HasError = true;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync() =>
        await Shell.Current.GoToAsync("//Register");
}

