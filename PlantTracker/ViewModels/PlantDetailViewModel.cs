using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PlantTracker.Messages;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.ViewModels;

[QueryProperty(nameof(PlantId), "PlantId")]
[QueryProperty(nameof(PlantSummary), "PlantSummary")]
public partial class PlantDetailViewModel : BaseViewModel
{
    private readonly PlantService _plants;
    private readonly GardenService _garden;
    private readonly AuthService _auth;

    [ObservableProperty] private int _plantId;
    [ObservableProperty] private PlantSummaryDto? _plantSummary;
    [ObservableProperty] private PlantDetailDto? _detail;
    [ObservableProperty] private PlantingAdviceDto? _advice;
    [ObservableProperty] private bool _isInGarden;
    [ObservableProperty] private string _adviceEmoji = string.Empty;

    public string IndoorOutdoorText => Detail?.Indoor == true ? "Indoors" : "Outdoors";

    public PlantDetailViewModel(PlantService plants, GardenService garden, AuthService auth)
    {
        _plants = plants;
        _garden = garden;
        _auth = auth;
        Title = "Plant Details";
    }

    partial void OnPlantIdChanged(int value)
    {
        if (value > 0)
            LoadDetailCommand.ExecuteAsync(null);
    }

    partial void OnDetailChanged(PlantDetailDto? value) =>
        OnPropertyChanged(nameof(IndoorOutdoorText));

    [RelayCommand]
    public async Task LoadDetailAsync()
    {
        if (IsBusy || PlantId == 0) return;
        IsBusy = true;

        try
        {
            Detail = await _plants.GetDetailAsync(PlantId);
            if (Detail is not null)
                Title = Detail.CommonName;

            // Check if plant is already saved to garden
            var gardenPlants = await _garden.GetGardenAsync();
            IsInGarden = gardenPlants.Any(p => p.PlantId == PlantId);

            // Load zone advice using user's stored zip
            var user = await _auth.GetCurrentUserAsync();
            if (!string.IsNullOrWhiteSpace(user?.ZipCode))
            {
                Advice = await _plants.GetPlantingAdviceAsync(PlantId, user.ZipCode);
                AdviceEmoji = Advice?.Recommendation switch
                {
                    PlantingRecommendation.PlantNowOutdoors => "✅",
                    PlantingRecommendation.StartIndoors     => "🏠",
                    PlantingRecommendation.Wait             => "⏳",
                    PlantingRecommendation.ZoneNotCompatible => "❌",
                    _ => "🌱"
                };
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddToGardenAsync()
    {
        if (Detail is null && PlantSummary is null) return;

        var dto = new AddUserPlantDto
        {
            PlantId = PlantId,
            CommonName = Detail?.CommonName ?? PlantSummary?.CommonName ?? string.Empty,
            ScientificName = Detail?.ScientificName ?? PlantSummary?.ScientificName ?? string.Empty,
            ThumbnailUrl = Detail?.ImageUrl ?? PlantSummary?.ThumbnailUrl,
            WateringReminderEnabled = false,
            WateringFrequencyDays = Detail?.WateringFrequencyDays
        };

        var (success, _, error) = await _garden.AddPlantAsync(dto);

        if (success)
        {
            IsInGarden = true;
            WeakReferenceMessenger.Default.Send(new GardenPlantAddedMessage());
            await Shell.Current.DisplayAlertAsync("Added!", $"{dto.CommonName} has been added to your garden. 🌱", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Oops", error ?? "Could not add plant.", "OK");
        }
    }
}


