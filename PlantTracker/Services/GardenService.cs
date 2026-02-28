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

    public async Task<List<PlantPhotoDto>> GetPhotosAsync(int userPlantId)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _http.GetFromJsonAsync<List<PlantPhotoDto>>($"api/garden/{userPlantId}/photos") ?? [];
        }
        catch { return []; }
    }

    public async Task<(bool Success, string? Error)> AddPhotoAsync(int userPlantId, PlantPhotoDto dto)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PostAsJsonAsync($"api/garden/{userPlantId}/photos", dto);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> DeletePhotoAsync(int userPlantId, int photoId)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.DeleteAsync($"api/garden/{userPlantId}/photos/{photoId}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> SetCoverPhotoAsync(int userPlantId, string imageData)
    {
        await SetAuthHeaderAsync();
        try
        {
            // We only want to update ThumbnailUrl; pass the existing plant data through with
            // a partial DTO — the API ignores null fields for care attributes.
            var response = await _http.PutAsJsonAsync($"api/garden/{userPlantId}", new UpdateUserPlantDto
            {
                ThumbnailUrl = imageData
            });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
