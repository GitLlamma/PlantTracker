using System.Text.Json.Serialization;

namespace PlantTracker.Api.Models;

// Perenual list response wrapper
public class PerenualListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];

    [JsonPropertyName("to")]
    public int To { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

// Search result item
public class PerenualPlantSummary
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("common_name")]
    public string CommonName { get; set; } = string.Empty;

    [JsonPropertyName("scientific_name")]
    public List<string> ScientificName { get; set; } = [];

    [JsonPropertyName("default_image")]
    public PerenualImage? DefaultImage { get; set; }

    [JsonPropertyName("cycle")]
    public string? Cycle { get; set; }

    [JsonPropertyName("watering")]
    public string? Watering { get; set; }
}

// Detail response
public class PerenualPlantDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("common_name")]
    public string CommonName { get; set; } = string.Empty;

    [JsonPropertyName("scientific_name")]
    public List<string> ScientificName { get; set; } = [];

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("default_image")]
    public PerenualImage? DefaultImage { get; set; }

    [JsonPropertyName("watering")]
    public string? Watering { get; set; }

    [JsonPropertyName("sunlight")]
    public List<string> Sunlight { get; set; } = [];

    [JsonPropertyName("cycle")]
    public string? Cycle { get; set; }

    [JsonPropertyName("care_level")]
    public string? CareLevel { get; set; }

    [JsonPropertyName("indoor")]
    public bool? Indoor { get; set; }

    [JsonPropertyName("growth_rate")]
    public string? GrowthRate { get; set; }

    [JsonPropertyName("flowering_season")]
    public string? FloweringSeason { get; set; }

    [JsonPropertyName("fruit_season")]
    public string? FruitSeason { get; set; }

    [JsonPropertyName("hardiness")]
    public PerenualHardiness? Hardiness { get; set; }

    [JsonPropertyName("dimension")]
    public string? Dimension { get; set; }
}

public class PerenualImage
{
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("regular_url")]
    public string? RegularUrl { get; set; }
}

public class PerenualHardiness
{
    [JsonPropertyName("min")]
    public string? Min { get; set; }

    [JsonPropertyName("max")]
    public string? Max { get; set; }
}

