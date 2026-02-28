using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class PlantDetailPage : ContentPage
{
    private readonly PlantDetailViewModel _vm;

    public PlantDetailPage(PlantDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        _vm.Reset();
    }
}

