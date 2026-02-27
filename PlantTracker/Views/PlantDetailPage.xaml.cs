using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class PlantDetailPage : ContentPage
{
    public PlantDetailPage(PlantDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

