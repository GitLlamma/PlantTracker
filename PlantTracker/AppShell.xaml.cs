using PlantTracker.Services;
using PlantTracker.Views;

namespace PlantTracker;

public partial class AppShell : Shell
{
    private readonly AuthService _auth;
    private bool _startupCheckDone;

    public AppShell(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;

        // Register routes for pages not in the tab bar
        Routing.RegisterRoute("PlantDetail", typeof(PlantDetailPage));
        Routing.RegisterRoute("AddCustomPlant", typeof(AddCustomPlantPage));
        Routing.RegisterRoute("EditPlant", typeof(EditPlantPage));
        Routing.RegisterRoute("PlantDiseases", typeof(PlantDiseasesPage));
        Routing.RegisterRoute("PlantGallery", typeof(PlantGalleryPage));

        Navigating += OnShellNavigating;
    }

    // Tab routes as defined in AppShell.xaml
    private static readonly HashSet<string> TabRoutes = ["//Search", "//MyGarden", "//Reminders", "//Settings"];
    private bool _isHandlingTabSwitch;

    private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        // Guard against re-entrant calls triggered by our own GoToAsync below
        if (_isHandlingTabSwitch) return;

        // If navigating to a root tab route while a sub-page is on the stack,
        // cancel the default navigation and replace it with a direct absolute
        // GoToAsync — this clears the stack atomically with no flash.
        if (TabRoutes.Contains(e.Target.Location.OriginalString)
            && Navigation.NavigationStack.Count > 1)
        {
            e.Cancel();
            _isHandlingTabSwitch = true;
            _ = GoToAsync(e.Target.Location.OriginalString, false)
                .ContinueWith(_ => _isHandlingTabSwitch = false);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_startupCheckDone) return;
        _startupCheckDone = true;

        try
        {
            if (!await _auth.IsLoggedInAsync())
                await GoToAsync("//Login");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Something went wrong during startup: {ex.Message}", "OK");
        }
    }
}