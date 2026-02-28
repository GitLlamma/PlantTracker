using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PlantTracker.Messages;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.ViewModels;

[QueryProperty(nameof(PlantSummary), "PlantSummary")]
[QueryProperty(nameof(UserPlant), "UserPlant")]
[QueryProperty(nameof(PlantId), "PlantId")]
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
    [ObservableProperty] private UserPlantDto? _userPlant;
    [ObservableProperty] private int _userPlantId; // DB row ID — needed for gallery/photos

    // ── Inline edit state ────────────────────────────────────────────────────
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _editNickname = string.Empty;
    [ObservableProperty] private string _editCommonName = string.Empty;
    [ObservableProperty] private string _editScientificName = string.Empty;
    [ObservableProperty] private string _editSelectedWatering = string.Empty;
    [ObservableProperty] private string _editSelectedSunlight = string.Empty;
    [ObservableProperty] private string _editSelectedCycle = string.Empty;
    [ObservableProperty] private string _editSelectedCareLevel = string.Empty;

    public List<string> WateringOptions { get; } = ["Frequent", "Average", "Minimum", "None"];
    public List<string> SunlightOptions { get; } = ["Full Sun", "Part Sun/Part Shade", "Part Shade", "Full Shade", "Filtered Shade"];
    public List<string> CycleOptions { get; } = ["Perennial", "Annual", "Biennial", "Ephemeral"];
    public List<string> CareLevelOptions { get; } = ["Low", "Medium", "High"];

    public string EditWateringDescription => EditSelectedWatering switch
    {
        "Frequent" => "Water every 1–2 days. Keep soil consistently moist.",
        "Average"  => "Water every 5–7 days. Allow top inch of soil to dry between waterings.",
        "Minimum"  => "Water every 2 weeks or less. Drought-tolerant; avoid overwatering.",
        "None"     => "No regular watering needed.",
        _          => string.Empty
    };

    public string EditSunlightDescription => EditSelectedSunlight switch
    {
        "Full Sun"            => "6+ hours of direct sunlight per day.",
        "Part Sun/Part Shade" => "3–6 hours of direct sun. Tolerates both sunny and shadier spots.",
        "Part Shade"          => "3–6 hours of sunlight, preferably morning sun with afternoon shade.",
        "Full Shade"          => "Fewer than 3 hours of direct sunlight per day.",
        "Filtered Shade"      => "Dappled light through a tree canopy throughout the day.",
        _                     => string.Empty
    };

    public string EditCycleDescription => EditSelectedCycle switch
    {
        "Perennial"  => "Lives for more than two years. Regrows each season.",
        "Annual"     => "Completes its full life cycle in one growing season.",
        "Biennial"   => "Takes two years to complete its life cycle.",
        "Ephemeral"  => "Short-lived; completes its cycle in weeks and goes dormant.",
        _            => string.Empty
    };

    public string EditCareLevelDescription => EditSelectedCareLevel switch
    {
        "Low"    => "Very forgiving. Tolerates neglect and irregular care.",
        "Medium" => "Needs regular but straightforward attention.",
        "High"   => "Requires frequent, specific care.",
        _        => string.Empty
    };

    // True only for custom plants (PlantId == 0) that are in the garden AND currently editing
    public bool IsEditableCustomPlant => IsEditing && IsInGarden && UserPlantId > 0 && PlantId == 0;
    // Notes are editable for all saved garden plants
    public bool IsEditableAnyPlant => IsInGarden && UserPlantId > 0;
    // Edit button is shown when the plant is editable and the inline editor is not already open
    public bool ShowEditButton => IsEditableAnyPlant && !IsEditing;

    partial void OnEditSelectedWateringChanged(string value) => OnPropertyChanged(nameof(EditWateringDescription));
    partial void OnEditSelectedSunlightChanged(string value) => OnPropertyChanged(nameof(EditSunlightDescription));
    partial void OnEditSelectedCycleChanged(string value)    => OnPropertyChanged(nameof(EditCycleDescription));
    partial void OnEditSelectedCareLevelChanged(string value)=> OnPropertyChanged(nameof(EditCareLevelDescription));

    public string IndoorOutdoorText => Detail?.Indoor == true ? "Indoors" : "Outdoors";

    public bool HasPerenualData => PlantId > 0;

    /// <summary>True when the plant is in the garden and has a known DB row ID — gallery button visible.</summary>
    public bool IsInGardenAndSaved => IsInGarden && UserPlantId > 0;

    /// <summary>The user's personal nickname for this plant, if set.</summary>
    public string? Nickname => UserPlant?.Nickname;

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

    /// <summary>Clears all visible state. Called when navigating away via a tab switch.</summary>
    public void Reset()
    {
        // Disable editing first — IsEditableCustomPlant depends on IsEditing,
        // so clearing this before any other property change prevents pickers
        // from briefly becoming visible during the reset sequence.
        IsEditing = false;
        ClearEditFields();

        // Clear garden membership first so OnPlantIdChanged's LoadDetailAsync
        // guard (UserPlantId == 0) is already correct before PlantId changes.
        IsInGarden = false;
        UserPlantId = 0;
        Advice = null;
        AdviceEmoji = string.Empty;
        Detail = null;
        PlantSummary = null;
        // Setting UserPlant = null triggers OnUserPlantChanged which returns early on null — safe.
        UserPlant = null;
        // Set PlantId last — OnPlantIdChanged fires LoadDetailAsync only when value > 0,
        // so setting to 0 is a no-op for loading but clears the state.
        PlantId = 0;
        Title = "Plant Details";
    }

    private void ClearEditFields()
    {
        EditCommonName        = string.Empty;
        EditScientificName    = string.Empty;
        EditNotes             = string.Empty;
        EditNickname          = string.Empty;
        // Set to empty string so no picker item is selected — avoids the MAUI
        // bug where setting SelectedItem to a valid value triggers the dropdown
        // to open even when the Picker is IsVisible=false.
        EditSelectedWatering  = string.Empty;
        EditSelectedSunlight  = string.Empty;
        EditSelectedCycle     = string.Empty;
        EditSelectedCareLevel = string.Empty;
    }

    partial void OnUserPlantIdChanged(int value)
    {
        OnPropertyChanged(nameof(IsInGardenAndSaved));
        OnPropertyChanged(nameof(IsEditableAnyPlant));
        OnPropertyChanged(nameof(IsEditableCustomPlant));
        OnPropertyChanged(nameof(ShowEditButton));
    }

    partial void OnIsInGardenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsInGardenAndSaved));
        OnPropertyChanged(nameof(IsEditableAnyPlant));
        OnPropertyChanged(nameof(IsEditableCustomPlant));
        OnPropertyChanged(nameof(ShowEditButton));
    }

    partial void OnPlantIdChanged(int value)
    {
        OnPropertyChanged(nameof(HasPerenualData));
        OnPropertyChanged(nameof(IsEditableCustomPlant));
        OnPropertyChanged(nameof(ShowEditButton));
        if (value > 0)
            LoadDetailCommand.ExecuteAsync(null);
    }

    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEditButton));
        OnPropertyChanged(nameof(IsEditableCustomPlant));
    }

    [RelayCommand]
    private void BeginEdit()
    {
        // Populate edit fields from current Detail / UserPlant
        EditCommonName        = Detail?.CommonName ?? string.Empty;
        EditScientificName    = Detail?.ScientificName ?? string.Empty;
        EditNotes             = Detail?.Description ?? string.Empty;
        EditNickname          = UserPlant?.Nickname ?? string.Empty;
        EditSelectedWatering  = WateringOptions.Contains(Detail?.Watering ?? "") ? Detail!.Watering! : "Average";
        EditSelectedSunlight  = SunlightOptions.Contains(Detail?.Sunlight?.FirstOrDefault() ?? "")
                                 ? Detail!.Sunlight!.First() : "Full Sun";
        EditSelectedCycle     = CycleOptions.Contains(Detail?.Cycle ?? "") ? Detail!.Cycle! : "Perennial";
        EditSelectedCareLevel = CareLevelOptions.Contains(Detail?.CareLevel ?? "") ? Detail!.CareLevel! : "Medium";
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearEditFields();
    }

    [RelayCommand]
    private async Task PickWateringAsync()
    {
        var result = await Shell.Current.DisplayActionSheet(
            "Watering Needs", "Cancel", null, [.. WateringOptions]);
        if (result is not null && result != "Cancel" && WateringOptions.Contains(result))
            EditSelectedWatering = result;
    }

    [RelayCommand]
    private async Task PickSunlightAsync()
    {
        var result = await Shell.Current.DisplayActionSheet(
            "Sunlight", "Cancel", null, [.. SunlightOptions]);
        if (result is not null && result != "Cancel" && SunlightOptions.Contains(result))
            EditSelectedSunlight = result;
    }

    [RelayCommand]
    private async Task PickCycleAsync()
    {
        var result = await Shell.Current.DisplayActionSheet(
            "Life Cycle", "Cancel", null, [.. CycleOptions]);
        if (result is not null && result != "Cancel" && CycleOptions.Contains(result))
            EditSelectedCycle = result;
    }

    [RelayCommand]
    private async Task PickCareLevelAsync()
    {
        var result = await Shell.Current.DisplayActionSheet(
            "Care Level", "Cancel", null, [.. CareLevelOptions]);
        if (result is not null && result != "Cancel" && CareLevelOptions.Contains(result))
            EditSelectedCareLevel = result;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (IsBusy || UserPlantId == 0) return;
        IsBusy = true;

        try
        {
            // Retrieve the current saved plant to preserve watering reminder state
            var gardenPlants = await _garden.GetGardenAsync();
            var existing = gardenPlants.FirstOrDefault(p => p.Id == UserPlantId);

            var dto = new UpdateUserPlantDto
            {
                Notes                  = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim(),
                Nickname               = string.IsNullOrWhiteSpace(EditNickname) ? null : EditNickname.Trim(),
                WateringReminderEnabled = existing?.WateringReminderEnabled ?? false,
                WateringFrequencyDays  = existing?.WateringFrequencyDays,
                LastWateredAt          = existing?.LastWateredAt,
                // Custom-plant-only fields — API ignores these for Perenual plants
                CommonName       = EditCommonName.Trim(),
                ScientificName   = EditScientificName.Trim(),
                Watering         = EditSelectedWatering,
                Sunlight         = EditSelectedSunlight,
                Cycle            = EditSelectedCycle,
                CareLevel        = EditSelectedCareLevel
            };

            var (success, updated, error) = await _garden.UpdatePlantAsync(UserPlantId, dto);

            if (success && updated is not null)
            {
                // Reflect changes in the Detail displayed on-screen
                if (Detail is not null)
                {
                    Detail.Description    = updated.Notes;
                    Detail.CommonName     = updated.CommonName;
                    Detail.ScientificName = updated.ScientificName;
                    Detail.Watering       = updated.Watering;
                    Detail.Sunlight       = string.IsNullOrEmpty(updated.Sunlight) ? [] : [updated.Sunlight];
                    Detail.Cycle          = updated.Cycle;
                    Detail.CareLevel      = updated.CareLevel;
                    OnDetailChanged(Detail);
                }

                // Reflect nickname back onto the UserPlant so BeginEdit picks it up next time
                if (UserPlant is not null)
                    UserPlant.Nickname = updated.Nickname;
                OnPropertyChanged(nameof(Nickname));

                Title = updated.CommonName;
                IsEditing = false;
                ClearEditFields();
                WeakReferenceMessenger.Default.Send(new GardenPlantAddedMessage());
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", error ?? "Could not save changes.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnUserPlantChanged(UserPlantDto? value)
    {
        if (value is null) return;

        // Always mark as in-garden so the edit button becomes visible immediately
        IsInGarden = true;
        UserPlantId = value.Id;
        OnPropertyChanged(nameof(IsInGardenAndSaved));
        OnPropertyChanged(nameof(Nickname));

        // For custom plants (PlantId == 0) there is no Perenual detail to load,
        // so build the PlantDetailDto directly from the saved UserPlantDto.
        // For Perenual plants, leave Detail alone — LoadDetailAsync will populate it.
        if (value.PlantId == 0)
        {
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
                Sunlight = string.IsNullOrEmpty(value.Sunlight) ? [] : [value.Sunlight],
                Cycle = value.Cycle,
                CareLevel = value.CareLevel
            };
        }
    }

    partial void OnDetailChanged(PlantDetailDto? value)
    {
        OnPropertyChanged(nameof(IndoorOutdoorText));
        OnPropertyChanged(nameof(GrowthRateDetail));
        OnPropertyChanged(nameof(SunlightDescriptions));
    }


    [RelayCommand]
    private async Task GoToDiseasesAsync()
    {
        await Shell.Current.GoToAsync("PlantDiseases", new Dictionary<string, object>
        {
            { "PlantId", PlantId },
            { "PlantName", Detail?.CommonName ?? PlantSummary?.CommonName ?? string.Empty }
        });
    }

    [RelayCommand]
    private async Task GoToGalleryAsync()
    {
        await Shell.Current.GoToAsync("PlantGallery", new Dictionary<string, object>
        {
            { "UserPlantId", UserPlantId },
            { "PlantName", Detail?.CommonName ?? PlantSummary?.CommonName ?? string.Empty }
        });
    }

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

            // Only query the garden if UserPlantId wasn't already provided via
            // the UserPlant query property (e.g. navigating from My Garden).
            // This prevents overwriting IsInGarden/UserPlantId that were already set.
            if (UserPlantId == 0)
            {
                var gardenPlants = await _garden.GetGardenAsync();
                var savedPlant = gardenPlants.FirstOrDefault(p => p.PlantId == PlantId);
                IsInGarden = savedPlant is not null;
                UserPlantId = savedPlant?.Id ?? 0;
                OnPropertyChanged(nameof(IsInGardenAndSaved));
            }

            // Load zone advice using user's stored zip
            var user = await _auth.GetCurrentUserAsync();
            if (!string.IsNullOrWhiteSpace(user?.ZipCode))
            {
                Advice = await _plants.GetPlantingAdviceAsync(PlantId, user.ZipCode);
                AdviceEmoji = Advice?.Recommendation switch
                {
                    PlantingRecommendation.PlantNowOutdoors  => "✅",
                    PlantingRecommendation.StartIndoors      => "🏠",
                    PlantingRecommendation.Wait              => "⏳",
                    PlantingRecommendation.ZoneNotCompatible => "❌",
                    _                                        => "🌱"
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

        var (success, addedPlant, error) = await _garden.AddPlantAsync(dto);

        if (success)
        {
            IsInGarden = true;
            UserPlantId = addedPlant?.Id ?? 0;
            OnPropertyChanged(nameof(IsInGardenAndSaved));
            WeakReferenceMessenger.Default.Send(new GardenPlantAddedMessage());
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Oops", error ?? "Could not add plant.", "OK");
        }
    }
}

