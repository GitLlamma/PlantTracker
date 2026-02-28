using System.Net.Http.Headers;
using System.Net.Http.Json;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.Services;

public class GardenService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    // In-memory cache — populated on first load, kept in sync by all mutating operations.
    private List<UserPlantDto>? _cache;

    public GardenService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    /// <summary>Clears the garden cache — call on logout.</summary>
    public void ClearCache() => _cache = null;

    private async Task SetAuthHeaderAsync()
    {
        var token = await _auth.GetTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Returns cached garden data immediately if available, then fetches fresh data
    /// from the server in the background and notifies via the returned task.
    /// If no cache exists yet, waits for the first network fetch before returning.
    /// </summary>
    public async Task<List<UserPlantDto>> GetGardenAsync()
    {
        if (_cache is not null)
        {
            // Return cache immediately, refresh in the background
            _ = RefreshCacheAsync();
            return _cache;
        }

        // First load — must wait for network
        await RefreshCacheAsync();
        return _cache ?? [];
    }

    /// <summary>Fetches fresh data from the server and updates the cache.</summary>
    public async Task RefreshCacheAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            var result = await _http.GetFromJsonAsync<List<UserPlantDto>>("api/garden");
            _cache = result ?? [];
        }
        catch
        {
            // Leave existing cache intact if the refresh fails
        }
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
            if (plant is not null)
            {
                _cache ??= [];
                _cache.Add(plant);
            }
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
            if (plant is not null && _cache is not null)
            {
                var idx = _cache.FindIndex(p => p.Id == id);
                if (idx >= 0) _cache[idx] = plant;
            }
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
        if (response.IsSuccessStatusCode)
            _cache?.RemoveAll(p => p.Id == id);
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, UserPlantDto? Plant)> MarkWateredAsync(int id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsync($"api/garden/{id}/watered", null);
        if (!response.IsSuccessStatusCode) return (false, null);
        var plant = await response.Content.ReadFromJsonAsync<UserPlantDto>();
        if (plant is not null && _cache is not null)
        {
            var idx = _cache.FindIndex(p => p.Id == id);
            if (idx >= 0) _cache[idx] = plant;
        }
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
            var response = await _http.PutAsJsonAsync($"api/garden/{userPlantId}", new UpdateUserPlantDto
            {
                ThumbnailUrl = imageData
            });
            if (response.IsSuccessStatusCode && _cache is not null)
            {
                var cached = _cache.FirstOrDefault(p => p.Id == userPlantId);
                if (cached is not null) cached.ThumbnailUrl = imageData;
            }
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
