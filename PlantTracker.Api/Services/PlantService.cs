using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PlantTracker.Api.Models;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.Api.Services;

public class PlantService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Watering frequency mapping from Perenual string values
    private static readonly Dictionary<string, int> WateringFrequencyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Frequent",  2 },
        { "Average",   7 },
        { "Minimum",  14 },
        { "None",     30 }
    };

    public PlantService(HttpClient http, IMemoryCache cache, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _apiKey = config["PerenualApiKey"] ?? throw new InvalidOperationException("PerenualApiKey not configured.");
    }

    public async Task<List<PlantSummaryDto>> SearchAsync(string query)
    {
        var cacheKey = $"search:{query.ToLower().Trim()}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

            var url = $"https://perenual.com/api/v2/species-list?key={_apiKey}&q={Uri.EscapeDataString(query)}&page=1";
            var response = await _http.GetFromJsonAsync<PerenualListResponse<PerenualPlantSummary>>(url, JsonOptions);

            return response?.Data.Select(MapToSummary).ToList() ?? [];
        }) ?? [];
    }

    public async Task<PlantDetailDto?> GetDetailAsync(int plantId)
    {
        var cacheKey = $"detail:{plantId}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);

            var url = $"https://perenual.com/api/v2/species/details/{plantId}?key={_apiKey}";
            var detail = await _http.GetFromJsonAsync<PerenualPlantDetail>(url, JsonOptions);

            return detail is null ? null : MapToDetail(detail);
        });
    }

    private static PlantSummaryDto MapToSummary(PerenualPlantSummary p) => new()
    {
        Id = p.Id,
        CommonName = p.CommonName,
        ScientificName = p.ScientificName.FirstOrDefault() ?? string.Empty,
        ThumbnailUrl = p.DefaultImage?.Thumbnail,
        Cycle = p.Cycle,
        Watering = p.Watering
    };

    private static PlantDetailDto MapToDetail(PerenualPlantDetail p) => new()
    {
        Id = p.Id,
        CommonName = p.CommonName,
        ScientificName = p.ScientificName.FirstOrDefault() ?? string.Empty,
        ImageUrl = p.DefaultImage?.RegularUrl ?? p.DefaultImage?.Thumbnail,
        Description = p.Description,
        Watering = p.Watering,
        WateringFrequencyDays = p.Watering is not null && WateringFrequencyMap.TryGetValue(p.Watering, out var days)
            ? days : null,
        Sunlight = p.Sunlight,
        Cycle = p.Cycle,
        CareLevel = p.CareLevel,
        Indoor = p.Indoor,
        GrowthRate = p.GrowthRate,
        Dimension = p.Dimension,
        HardinessZoneMin = TryParseZone(p.Hardiness?.Min),
        HardinessZoneMax = TryParseZone(p.Hardiness?.Max),
    };

    private static int? TryParseZone(string? value) =>
        int.TryParse(value, out var z) ? z : null;
}


