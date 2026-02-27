using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class AddCustomPlantPage : ContentPage
{
    public AddCustomPlantPage(AddCustomPlantViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

