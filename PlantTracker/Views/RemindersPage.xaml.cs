using PlantTracker.ViewModels;

namespace PlantTracker.Views;

public partial class RemindersPage : ContentPage
{
    private readonly RemindersViewModel _vm;

    public RemindersPage(RemindersViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadRemindersCommand.ExecuteAsync(null);
    }
}
