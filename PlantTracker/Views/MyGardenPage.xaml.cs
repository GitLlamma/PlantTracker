using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class MyGardenPage : ContentPage
{
    private readonly MyGardenViewModel _vm;

    private const double FarCloudRate  = 0.04;
    private const double NearCloudRate = 0.09;

    // Grass overlay height — footer only needed when list content exceeds this threshold
    private const double GrassHeight = 120;

    public MyGardenPage(MyGardenViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MyGardenViewModel.IsEmpty))
                Dispatcher.Dispatch(UpdateFooterVisibility);
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_vm.Plants.Count > 0)
        {
            // Cache already populated — fire load in background, return immediately
            _ = _vm.LoadGardenCommand.ExecuteAsync(null);
        }
        else
        {
            // First load — wait for data before showing the page
            await _vm.LoadGardenCommand.ExecuteAsync(null);
        }

        // Defer footer check so it doesn't force a layout pass synchronously
        Dispatcher.Dispatch(UpdateFooterVisibility);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateFooterVisibility();
    }

    private void OnGardenListSizeChanged(object sender, EventArgs e) =>
        UpdateFooterVisibility();

    private void OnGardenScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        CloudsFar.TranslationY  = -e.VerticalOffset * FarCloudRate;
        CloudsNear.TranslationY = -e.VerticalOffset * NearCloudRate;
    }

    private void UpdateFooterVisibility()
    {
        var pageHeight = Height;
        if (pageHeight <= 0 || Width <= 0) return;

        // Measure the CollectionView's natural (unconstrained) height — i.e. how tall
        // it would be if it didn't have to fit on screen. If that exceeds the visible
        // area (page height minus the grass overlay), the list needs scrolling.
        var naturalHeight = GardenList.Measure(Width, double.PositiveInfinity).Height;
        _vm.FooterVisible = !_vm.IsEmpty && naturalHeight > (pageHeight - GrassHeight);
    }
}

