﻿using PlantTracker.Services;
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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_startupCheckDone) return;
        _startupCheckDone = true;

        if (!await _auth.IsLoggedInAsync())
            await GoToAsync("//Login");
    }
}