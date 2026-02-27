using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class MyGardenPage : ContentPage
{
    private readonly MyGardenViewModel _vm;

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
}

