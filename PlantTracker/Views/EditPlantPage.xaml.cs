using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class EditPlantPage : ContentPage
{
    private readonly EditPlantViewModel _vm;

    public EditPlantPage(EditPlantViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}

