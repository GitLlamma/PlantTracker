using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class PlantGalleryPage : ContentPage
{
    public PlantGalleryPage(PlantGalleryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

