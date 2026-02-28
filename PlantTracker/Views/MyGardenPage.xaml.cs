using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class MyGardenPage : ContentPage
{
    private readonly MyGardenViewModel _vm;

    // Parallax rates — how many pixels each cloud layer moves per pixel scrolled.
    // Cards scroll at 1.0. Far clouds scroll slowest (appear furthest away).
    private const double FarCloudRate  = 0.04;
    private const double NearCloudRate = 0.09;

    public MyGardenPage(MyGardenViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadGardenCommand.ExecuteAsync(null);
    }

    private void OnGardenScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        // Translate each cloud layer upward as the list scrolls down.
        // A lower rate means the layer moves less — making it appear further away.
        CloudsFar.TranslationY  = -e.VerticalOffset * FarCloudRate;
        CloudsNear.TranslationY = -e.VerticalOffset * NearCloudRate;
    }
}

