using System.Net.Http.Headers;
using System.Net.Http.Json;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.Services;

public class GardenService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public GardenService(HttpClient http, AuthService auth)
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

    public async Task<List<UserPlantDto>> GetGardenAsync()
    {
        await SetAuthHeaderAsync();
        var result = await _http.GetFromJsonAsync<List<UserPlantDto>>("api/garden");
        return result ?? [];
    }

    public async Task<(bool Success, UserPlantDto? Plant, string? Error)> AddPlantAsync(AddUserPlantDto dto)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PostAsJsonAsync("api/garden", dto);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return (false, null, "This plant is already in your garden.");
            if (!response.IsSuccessStatusCode)
                return (false, null, "Failed to add plant.");
            var plant = await response.Content.ReadFromJsonAsync<UserPlantDto>();
            return (true, plant, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, UserPlantDto? Plant, string? Error)> UpdatePlantAsync(int id, UpdateUserPlantDto dto)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PutAsJsonAsync($"api/garden/{id}", dto);
            if (!response.IsSuccessStatusCode)
                return (false, null, "Failed to update plant.");
            var plant = await response.Content.ReadFromJsonAsync<UserPlantDto>();
            return (true, plant, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<bool> RemovePlantAsync(int id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/garden/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, UserPlantDto? Plant)> MarkWateredAsync(int id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsync($"api/garden/{id}/watered", null);
        if (!response.IsSuccessStatusCode) return (false, null);
        var plant = await response.Content.ReadFromJsonAsync<UserPlantDto>();
        return (true, plant);
    }
}

