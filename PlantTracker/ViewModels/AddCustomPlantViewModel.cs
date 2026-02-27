using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PlantTracker.Messages;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.ViewModels;

public partial class AddCustomPlantViewModel : BaseViewModel
{
    private readonly GardenService _garden;

    // ── Form fields ──────────────────────────────────────────────────────────
    [ObservableProperty] private string _commonName = string.Empty;
    [ObservableProperty] private string _scientificName = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    [ObservableProperty] private string _selectedWatering = "Average";
    [ObservableProperty] private string _selectedSunlight = "Full Sun";
    [ObservableProperty] private string _selectedCycle = "Perennial";
    [ObservableProperty] private string _selectedCareLevel = "Medium";

    // ── Picker options ───────────────────────────────────────────────────────
    public List<string> WateringOptions { get; } =
        ["Frequent", "Average", "Minimum", "None"];

    public List<string> SunlightOptions { get; } =
        ["Full Sun", "Part Sun/Part Shade", "Part Shade", "Full Shade", "Filtered Shade"];

    public List<string> CycleOptions { get; } =
        ["Perennial", "Annual", "Biennial", "Ephemeral"];

    public List<string> CareLevelOptions { get; } =
        ["Low", "Medium", "High"];

    // ── Computed descriptions ────────────────────────────────────────────────
    public string WateringDescription => SelectedWatering switch
    {
        "Frequent" => "Water every 1–2 days. Keep soil consistently moist.",
        "Average"  => "Water every 5–7 days. Allow top inch of soil to dry between waterings.",
        "Minimum"  => "Water every 2 weeks or less. Drought-tolerant; avoid overwatering.",
        "None"     => "No regular watering needed. Plant sustains itself from rainfall or stored moisture.",
        _          => string.Empty
    };

    public string SunlightDescription => SelectedSunlight switch
    {
        "Full Sun"             => "6+ hours of direct sunlight per day.",
        "Part Sun/Part Shade"  => "3–6 hours of direct sun. Tolerates both sunny and shadier spots.",
        "Part Shade"           => "3–6 hours of sunlight, preferably morning sun with afternoon shade.",
        "Full Shade"           => "Fewer than 3 hours of direct sunlight per day.",
        "Filtered Shade"       => "Dappled light through a tree canopy throughout the day.",
        _                      => string.Empty
    };

    public string CycleDescription => SelectedCycle switch
    {
        "Perennial"  => "Lives for more than two years. Regrows each season.",
        "Annual"     => "Completes its full life cycle in one growing season.",
        "Biennial"   => "Takes two years to complete its life cycle.",
        "Ephemeral"  => "Short-lived; completes its cycle in weeks and goes dormant.",
        _            => string.Empty
    };

    public string CareLevelDescription => SelectedCareLevel switch
    {
        "Low"    => "Very forgiving. Tolerates neglect and irregular care.",
        "Medium" => "Needs regular but straightforward attention.",
        "High"   => "Requires frequent, specific care — ideal for experienced gardeners.",
        _        => string.Empty
    };

    // Notify description properties when pickers change
    partial void OnSelectedWateringChanged(string value)  => OnPropertyChanged(nameof(WateringDescription));
    partial void OnSelectedSunlightChanged(string value)  => OnPropertyChanged(nameof(SunlightDescription));
    partial void OnSelectedCycleChanged(string value)     => OnPropertyChanged(nameof(CycleDescription));
    partial void OnSelectedCareLevelChanged(string value) => OnPropertyChanged(nameof(CareLevelDescription));

    public AddCustomPlantViewModel(GardenService garden)
    {
        _garden = garden;
        Title = "Add Custom Plant";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(CommonName))
        {
            await Shell.Current.DisplayAlertAsync("Required", "Please enter a plant name.", "OK");
            return;
        }

        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // PlantId = 0 signals a custom (non-Perenual) plant
            var dto = new AddUserPlantDto
            {
                PlantId = 0,
                CommonName = CommonName.Trim(),
                ScientificName = ScientificName.Trim(),
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                WateringReminderEnabled = false,
                Watering = SelectedWatering,
                Sunlight = SelectedSunlight,
                Cycle = SelectedCycle,
                CareLevel = SelectedCareLevel
            };

            var (success, _, error) = await _garden.AddPlantAsync(dto);

            if (success)
            {
                WeakReferenceMessenger.Default.Send(new GardenPlantAddedMessage());
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", error ?? "Could not add plant.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}

