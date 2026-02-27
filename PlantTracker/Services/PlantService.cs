using System.Net.Http.Headers;
using System.Net.Http.Json;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.Services;

public class PlantService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public PlantService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _auth.GetTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<PlantSummaryDto>> SearchAsync(string query)
    {
        await SetAuthHeaderAsync();
        try
        {
            var results = await _http.GetFromJsonAsync<List<PlantSummaryDto>>(
                $"api/plants/search?q={Uri.EscapeDataString(query)}");
            return results ?? [];
        }
        catch { return []; }
    }

    public async Task<PlantDetailDto?> GetDetailAsync(int plantId)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _http.GetFromJsonAsync<PlantDetailDto>($"api/plants/{plantId}");
        }
        catch { return null; }
    }

    public async Task<PlantingAdviceDto?> GetPlantingAdviceAsync(int plantId, string? zipCode = null)
    {
        await SetAuthHeaderAsync();
        try
        {
            var url = $"api/plants/{plantId}/advice";
            if (!string.IsNullOrWhiteSpace(zipCode))
                url += $"?zipCode={Uri.EscapeDataString(zipCode)}";

            return await _http.GetFromJsonAsync<PlantingAdviceDto>(url);
        }
        catch { return null; }
    }

    public async Task<int?> GetZoneAsync(string zipCode)
    {
        await SetAuthHeaderAsync();
        try
        {
            var result = await _http.GetFromJsonAsync<ZoneResult>(
                $"api/plants/zone?zipCode={Uri.EscapeDataString(zipCode)}");
            return result?.Zone;
        }
        catch { return null; }
    }

    private class ZoneResult
    {
        public int Zone { get; set; }
    }
}

