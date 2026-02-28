using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class PlantDiseasesPage : ContentPage
{
    public PlantDiseasesPage(PlantDiseasesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

