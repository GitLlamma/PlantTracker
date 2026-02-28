using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.ViewModels;

public partial class SearchViewModel : BaseViewModel
{
    private readonly PlantService _plants;
    private CancellationTokenSource? _debounceCts;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _hasSearched;
    [ObservableProperty] private bool _noResults;

    public ObservableCollection<PlantSummaryDto> Results { get; } = [];

    public SearchViewModel(PlantService plants)
    {
        _plants = plants;
        Title = "Search Plants";
    }

    partial void OnSearchQueryChanged(string value)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        if (value.Length <= 2)
        {
            Results.Clear();
            HasSearched = false;
            NoResults = false;
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
                await MainThread.InvokeOnMainThreadAsync(() => SearchCommand.ExecuteAsync(null));
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsBusy = true;
        HasSearched = false;
        Results.Clear();

        try
        {
            var results = await _plants.SearchAsync(SearchQuery);
            foreach (var plant in results)
                Results.Add(plant);

            HasSearched = true;
            NoResults = Results.Count == 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToDetailAsync(PlantSummaryDto plant)
    {
        await Shell.Current.GoToAsync("PlantDetail", new Dictionary<string, object>
        {
            { "PlantId", plant.Id },
            { "PlantSummary", plant }
        });
    }
}

