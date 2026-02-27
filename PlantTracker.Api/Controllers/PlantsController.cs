using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantTracker.Api.Services;
using PlantTracker.Shared.DTOs.Plants;

namespace PlantTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlantsController : ControllerBase
{
    private readonly PlantService _plants;
    private readonly ZoneService _zones;

    public PlantsController(PlantService plants, ZoneService zones)
    {
        _plants = plants;
        _zones = zones;
    }

    // GET api/plants/search?q=tomato
    [HttpGet("search")]
    public async Task<ActionResult<List<PlantSummaryDto>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Search query is required.");

        var results = await _plants.SearchAsync(q);
        return Ok(results);
    }

    // GET api/plants/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlantDetailDto>> GetDetail(int id)
    {
        var detail = await _plants.GetDetailAsync(id);
        if (detail is null) return NotFound();
        return Ok(detail);
    }

    // GET api/plants/{id}/advice?zipCode=90210
    [HttpGet("{id:int}/advice")]
    public async Task<ActionResult<PlantingAdviceDto>> GetPlantingAdvice(int id, [FromQuery] string? zipCode)
    {
        // Use zip from query param, or fall back to the user's stored zip from JWT
        var zip = zipCode ?? User.FindFirstValue("zipCode");

        if (string.IsNullOrWhiteSpace(zip))
            return BadRequest("Zip code is required. Pass ?zipCode=12345 or update your profile.");

        var detail = await _plants.GetDetailAsync(id);
        if (detail is null) return NotFound("Plant not found.");

        var userZone = await _zones.GetZoneForZipAsync(zip);
        if (userZone is null)
            return BadRequest("Could not determine hardiness zone for that zip code.");

        var advice = BuildAdvice(userZone.Value, detail);
        return Ok(advice);
    }

    // GET api/plants/zone?zipCode=90210
    [HttpGet("zone")]
    public async Task<ActionResult<object>> GetZone([FromQuery] string? zipCode)
    {
        var zip = zipCode ?? User.FindFirstValue("zipCode");

        if (string.IsNullOrWhiteSpace(zip))
            return BadRequest("Zip code is required.");

        var zone = await _zones.GetZoneForZipAsync(zip);
        if (zone is null)
            return BadRequest("Could not determine hardiness zone for that zip code.");

        return Ok(new { ZipCode = zip, Zone = zone });
    }

    private static PlantingAdviceDto BuildAdvice(int userZone, PlantDetailDto plant)
    {
        var zoneMin = plant.HardinessZoneMin;
        var zoneMax = plant.HardinessZoneMax;

        // If the plant has no zone data, give a generic response
        if (zoneMin is null || zoneMax is null)
        {
            return new PlantingAdviceDto
            {
                UserZone = userZone,
                PlantZoneMin = 0,
                PlantZoneMax = 0,
                Recommendation = PlantingRecommendation.StartIndoors,
                Message = "No hardiness zone data available for this plant. Starting indoors is generally safe."
            };
        }

        var recommendation = userZone < zoneMin
            ? PlantingRecommendation.ZoneNotCompatible
            : userZone > zoneMax
                ? PlantingRecommendation.ZoneNotCompatible
                : userZone == zoneMin
                    ? PlantingRecommendation.StartIndoors
                    : PlantingRecommendation.PlantNowOutdoors;

        var message = recommendation switch
        {
            PlantingRecommendation.PlantNowOutdoors =>
                $"Your zone ({userZone}) is within this plant's ideal range (zones {zoneMin}–{zoneMax}). You can plant outdoors.",
            PlantingRecommendation.StartIndoors =>
                $"Your zone ({userZone}) is on the edge of this plant's range (zones {zoneMin}–{zoneMax}). Start indoors and transplant after last frost.",
            PlantingRecommendation.ZoneNotCompatible =>
                $"Your zone ({userZone}) is outside this plant's ideal range (zones {zoneMin}–{zoneMax}). This plant may not survive in your climate.",
            _ => "No planting advice available."
        };

        return new PlantingAdviceDto
        {
            UserZone = userZone,
            PlantZoneMin = zoneMin.Value,
            PlantZoneMax = zoneMax.Value,
            Recommendation = recommendation,
            Message = message
        };
    }
}

