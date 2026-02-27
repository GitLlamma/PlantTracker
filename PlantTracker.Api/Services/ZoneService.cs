using Microsoft.Extensions.Caching.Memory;

namespace PlantTracker.Api.Services;

/// <summary>
/// Looks up the USDA hardiness zone for a US zip code.
/// Uses the public USDA Plant Hardiness Zone GeoJSON API — no key required.
/// </summary>
public class ZoneService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public ZoneService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    /// <summary>
    /// Returns the numeric hardiness zone (1-13) for a given zip code, or null if not found.
    /// </summary>
    public async Task<int?> GetZoneForZipAsync(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode)) return null;

        var cacheKey = $"zone:{zipCode}";
        return await _cache.GetOrCreateAsync<int?>(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30);

            try
            {
                // USDA hardiness zone API
                var url = $"https://phzmapi.org/{Uri.EscapeDataString(zipCode)}.json";
                var result = await _http.GetFromJsonAsync<ZoneApiResponse>(url);
                if (result?.Zone is null) return null;

                // Zone format is like "6b" or "10a" — extract the number
                var zoneStr = result.Zone.TrimEnd('a', 'b', 'A', 'B');
                return int.TryParse(zoneStr, out var zone) ? (int?)zone : null;
            }
            catch
            {
                return null;
            }
        });
    }

    private class ZoneApiResponse
    {
        public string? Zone { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("temperature_range")]
        public string? TemperatureRange { get; set; }
    }
}



