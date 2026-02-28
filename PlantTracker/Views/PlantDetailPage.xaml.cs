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

    public void Reset() => _vm.Reset();

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.CancelEditCommand.Execute(null);
    }
}

