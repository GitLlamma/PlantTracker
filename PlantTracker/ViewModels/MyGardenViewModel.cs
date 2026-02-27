using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.ViewModels;

public partial class MyGardenViewModel : BaseViewModel
{
    private readonly GardenService _garden;

    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<UserPlantDto> Plants { get; } = [];

    public MyGardenViewModel(GardenService garden)
    {
        _garden = garden;
        Title = "My Garden";
    }

    [RelayCommand]
    public async Task LoadGardenAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var plants = await _garden.GetGardenAsync();
            Plants.Clear();
            foreach (var p in plants)
                Plants.Add(p);

            IsEmpty = Plants.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MarkWateredAsync(UserPlantDto plant)
    {
        var (success, updated) = await _garden.MarkWateredAsync(plant.Id);
        if (success && updated is not null)
        {
            var index = Plants.IndexOf(plant);
            if (index >= 0) Plants[index] = updated;
        }
    }

    [RelayCommand]
    private async Task RemovePlantAsync(UserPlantDto plant)
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Remove Plant",
            $"Remove {plant.CommonName} from your garden?",
            "Remove", "Cancel");

        if (!confirm) return;

        var success = await _garden.RemovePlantAsync(plant.Id);
        if (success)
        {
            Plants.Remove(plant);
            IsEmpty = Plants.Count == 0;
        }
    }
}

