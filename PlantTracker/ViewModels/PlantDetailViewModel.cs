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
[QueryProperty(nameof(UserPlant), "UserPlant")]
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
    [ObservableProperty] private bool _isGrowthRateExpanded;
    [ObservableProperty] private UserPlantDto? _userPlant;

    public string IndoorOutdoorText => Detail?.Indoor == true ? "Indoors" : "Outdoors";

    /// <summary>Human-readable growth rate detail built from GrowthRate, Cycle, FloweringSeason, FruitSeason.</summary>
    public string GrowthRateDetail
    {
        get
        {
            if (Detail is null) return string.Empty;

            var lines = new List<string>();

            if (!string.IsNullOrEmpty(Detail.GrowthRate))
            {
                var rateDesc = Detail.GrowthRate.ToLower() switch
                {
                    "low"    => "This plant grows slowly, typically taking several years to reach maturity.",
                    "medium" => "This plant grows at a moderate pace, usually reaching maturity in 1–3 years.",
                    "high"   => "This plant is a fast grower and can reach maturity within a single season.",
                    _        => $"Growth rate: {Detail.GrowthRate}."
                };
                lines.Add(rateDesc);
            }

            if (!string.IsNullOrEmpty(Detail.Cycle))
                lines.Add($"Life cycle: {Detail.Cycle}.");

            if (!string.IsNullOrEmpty(Detail.FloweringSeason))
                lines.Add($"Flowering season: {Detail.FloweringSeason}.");

            if (!string.IsNullOrEmpty(Detail.FruitSeason))
                lines.Add($"Fruiting season: {Detail.FruitSeason}.");

            if (!string.IsNullOrEmpty(Detail.Dimension))
                lines.Add(Detail.Dimension);

            return string.Join("\n\n", lines);
        }
    }

    /// <summary>Maps each Perenual sunlight string to a plain-English description with hours.</summary>
    public List<string> SunlightDescriptions
    {
        get
        {
            if (Detail?.Sunlight is null || Detail.Sunlight.Count == 0) return [];

            return Detail.Sunlight.Select(s => s.ToLower() switch
            {
                "full sun"              => "☀️ Full Sun — 6+ hours of direct sunlight per day.",
                "part shade"            => "⛅ Part Shade — 3–6 hours of sunlight, preferably in the morning.",
                "partial shade"         => "⛅ Part Shade — 3–6 hours of sunlight, preferably in the morning.",
                "part sun/part shade"   => "🌤 Part Sun / Part Shade — 3–6 hours of direct sun, tolerates both.",
                "full shade"            => "🌑 Full Shade — fewer than 3 hours of direct sunlight per day.",
                "filtered shade"        => "🌿 Filtered Shade — dappled light through tree canopy all day.",
                "deep shade"            => "🌑 Deep Shade — little to no direct sunlight.",
                "sun-part shade"        => "🌤 Sun to Part Shade — thrives in full sun but tolerates partial shade.",
                _                       => $"☀️ {s}"
            }).ToList();
        }
    }

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

    partial void OnUserPlantChanged(UserPlantDto? value)
    {
        if (value is null) return;

        // Custom plant (PlantId == 0): build a PlantDetailDto from the saved UserPlantDto
        // so the detail page can display it without calling the Perenual API.
        IsInGarden = true;
        Title = value.CommonName;
        Detail = new PlantDetailDto
        {
            Id = 0,
            CommonName = value.CommonName,
            ScientificName = value.ScientificName,
            ImageUrl = value.ThumbnailUrl,
            Description = value.Notes,
            Watering = value.Watering,
            WateringFrequencyDays = value.WateringFrequencyDays,
            Sunlight = string.IsNullOrEmpty(value.Sunlight)
                ? []
                : [value.Sunlight],
            Cycle = value.Cycle,
            CareLevel = value.CareLevel
        };
    }

    partial void OnDetailChanged(PlantDetailDto? value)
    {
        OnPropertyChanged(nameof(IndoorOutdoorText));
        OnPropertyChanged(nameof(GrowthRateDetail));
        OnPropertyChanged(nameof(SunlightDescriptions));
    }

    [RelayCommand]
    private void ToggleGrowthRate() => IsGrowthRateExpanded = !IsGrowthRateExpanded;

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


