using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.Services;

public class GardenService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    private List<UserPlantDto>? _cache;

    // Path to the on-disk cache file in the app's private data directory
    private static string CacheFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, "garden_cache.json");

    public GardenService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
        LoadDiskCache();
    }

    /// <summary>
    /// Synchronously reads the disk cache at startup so GetCachedPlants()
    /// returns data immediately on the first call, even after a cold start.
    /// </summary>
    private void LoadDiskCache()
    {
        try
        {
            if (!File.Exists(CacheFilePath)) return;
            var json = File.ReadAllText(CacheFilePath);
            _cache = JsonSerializer.Deserialize<List<UserPlantDto>>(json);
        }
        catch
        {
            _cache = null;
        }
    }

    /// <summary>Persists the current cache to disk on a background thread.</summary>
    private void SaveDiskCache()
    {
        var snapshot = _cache;
        if (snapshot is null) return;
        _ = Task.Run(() =>
        {
            try
            {
                var json = JsonSerializer.Serialize(snapshot);
                File.WriteAllText(CacheFilePath, json);
            }
            catch { /* non-fatal */ }
        });
    }

    /// <summary>Clears both the in-memory and on-disk cache — call on logout.</summary>
    public void ClearCache()
    {
        _cache = null;
        try { File.Delete(CacheFilePath); } catch { }
    }

    /// <summary>Returns the current cache synchronously — empty list if not yet loaded.</summary>
    public List<UserPlantDto> GetCachedPlants() => _cache ?? [];

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
            // Persist to disk so the next cold start is instant
            SaveDiskCache();
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
                SaveDiskCache();
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
                SaveDiskCache();
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
        {
            _cache?.RemoveAll(p => p.Id == id);
            SaveDiskCache();
        }
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
            SaveDiskCache();
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
                if (cached is not null)
                {
                    cached.ThumbnailUrl = imageData;
                    SaveDiskCache();
                }
            }
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
