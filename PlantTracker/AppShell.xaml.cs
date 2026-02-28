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

        Navigated += OnShellNavigated;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        // When the user switches tabs, pop any pushed pages back to the tab root.
        if (e.Source == ShellNavigationSource.ShellSectionChanged ||
            e.Source == ShellNavigationSource.ShellItemChanged)
        {
            var nav = Current?.Navigation;
            if (nav != null && nav.NavigationStack.Count > 1)
                nav.PopToRootAsync(animated: false);
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