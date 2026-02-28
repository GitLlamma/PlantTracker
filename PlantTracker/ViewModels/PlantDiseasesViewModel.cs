using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.ViewModels;

[QueryProperty(nameof(PlantId), "PlantId")]
[QueryProperty(nameof(PlantName), "PlantName")]
public partial class PlantDiseasesViewModel : BaseViewModel
{
    private readonly PlantService _plants;

    [ObservableProperty] private int _plantId;
    [ObservableProperty] private string _plantName = string.Empty;
    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<PlantDiseaseDto> Diseases { get; } = [];

    public PlantDiseasesViewModel(PlantService plants)
    {
        _plants = plants;
        Title = "Diseases & Pests";
    }

    partial void OnPlantIdChanged(int value)
    {
        if (value > 0)
            LoadDiseasesCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LoadDiseasesAsync()
    {
        if (IsBusy || PlantId == 0) return;
        IsBusy = true;
        Diseases.Clear();
        IsEmpty = false;

        try
        {
            var results = await _plants.GetDiseasesAsync(PlantId);
            foreach (var d in results)
                Diseases.Add(d);

            IsEmpty = Diseases.Count == 0;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

