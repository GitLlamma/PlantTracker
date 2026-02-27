using PlantTracker.Services;

namespace PlantTracker;

public partial class AppShell : Shell
{
    private readonly AuthService _auth;

    public AppShell(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Redirect to login if the user has no stored token
        if (!_auth.IsLoggedIn)
            await GoToAsync("//Login");
    }
}