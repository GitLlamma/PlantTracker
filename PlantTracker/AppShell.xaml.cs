using PlantTracker.Services;
using PlantTracker.Views;

namespace PlantTracker;

public partial class AppShell : Shell
{
    private readonly AuthService _auth;
    private readonly IServiceProvider _services;
    private bool _startupCheckDone;

    public AppShell(AuthService auth, IServiceProvider services)
    {
        InitializeComponent();
        _auth = auth;
        _services = services;

        // Register routes for pages not in the tab bar
        Routing.RegisterRoute("PlantDetail", typeof(PlantDetailPage));
        Routing.RegisterRoute("AddCustomPlant", typeof(AddCustomPlantPage));
        Routing.RegisterRoute("EditPlant", typeof(EditPlantPage));
        Routing.RegisterRoute("PlantDiseases", typeof(PlantDiseasesPage));
        Routing.RegisterRoute("PlantGallery", typeof(PlantGalleryPage));

        Navigating += OnShellNavigating;
        Navigated += OnShellNavigated;
    }

    private PlantDetailPage? PlantDetail => _services.GetService<PlantDetailPage>();

    // Tab routes as defined in AppShell.xaml
    private static readonly HashSet<string> TabRoutes = ["//Search", "//MyGarden", "//Settings"];
    private bool _isHandlingTabSwitch;

    private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (_isHandlingTabSwitch) return;

        if (TabRoutes.Contains(e.Target.Location.OriginalString)
            && Navigation.NavigationStack.Count > 1)
        {
            // Reset detail page state before any frame renders — eliminates flash
            PlantDetail?.Reset();

            e.Cancel();
            _isHandlingTabSwitch = true;
            _ = GoToAsync(e.Target.Location.OriginalString, false)
                .ContinueWith(_ => _isHandlingTabSwitch = false);
        }
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        // Safety net: if a tab switch completed and sub-pages are still on the stack, pop them
        if (e.Source == ShellNavigationSource.ShellSectionChanged ||
            e.Source == ShellNavigationSource.ShellItemChanged)
        {
            PlantDetail?.Reset();
            if (Navigation.NavigationStack.Count > 1)
                Navigation.PopToRootAsync(animated: false);
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
