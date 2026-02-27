using PlantTracker.Services;
using PlantTracker.Views;

namespace PlantTracker;

public partial class AppShell : Shell
{
    private readonly AuthService _auth;

    public AppShell(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;

        // Register routes for pages not in the tab bar
        Routing.RegisterRoute("PlantDetail", typeof(PlantDetailPage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_auth.IsLoggedIn)
            await GoToAsync("//Login");
    }
}