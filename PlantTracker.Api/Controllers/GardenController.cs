using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantTracker.Api.Data;
using PlantTracker.Api.Models;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GardenController : ControllerBase
{
    private readonly AppDbContext _db;

    public GardenController(AppDbContext db)
    {
        _db = db;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private static UserPlantDto ToDto(UserPlant p) => new()
    {
        Id = p.Id,
        PlantId = p.PlantId,
        CommonName = p.CommonName,
        ScientificName = p.ScientificName,
        ThumbnailUrl = p.ThumbnailUrl,
        Notes = p.Notes,
        WateringReminderEnabled = p.WateringReminderEnabled,
        WateringFrequencyDays = p.WateringFrequencyDays,
        LastWateredAt = p.LastWateredAt,
        AddedAt = p.AddedAt,
        Watering = p.Watering,
        Sunlight = p.Sunlight,
        Cycle = p.Cycle,
        CareLevel = p.CareLevel
    };

    // ── GET api/garden ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserPlantDto>>> GetGarden()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var plants = await _db.UserPlants
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CommonName)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(plants);
    }

    // ── GET api/garden/{id} ───────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserPlantDto>> GetPlant(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var plant = await _db.UserPlants
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plant is null) return NotFound();

        return Ok(ToDto(plant));
    }

    // ── POST api/garden ───────────────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<UserPlantDto>> AddPlant([FromBody] AddUserPlantDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        // Prevent duplicates for Perenual plants — one entry per plant per user.
        // Custom plants (PlantId == 0) are always allowed since each is unique.
        if (dto.PlantId != 0)
        {
            var exists = await _db.UserPlants
                .AnyAsync(p => p.UserId == userId && p.PlantId == dto.PlantId);

            if (exists)
                return Conflict("This plant is already in your garden.");
        }

        var plant = new UserPlant
        {
            UserId = userId,
            PlantId = dto.PlantId,
            CommonName = dto.CommonName,
            ScientificName = dto.ScientificName,
            ThumbnailUrl = dto.ThumbnailUrl,
            Notes = dto.Notes,
            WateringReminderEnabled = dto.WateringReminderEnabled,
            WateringFrequencyDays = dto.WateringFrequencyDays,
            AddedAt = DateTime.UtcNow,
            Watering = dto.Watering,
            Sunlight = dto.Sunlight,
            Cycle = dto.Cycle,
            CareLevel = dto.CareLevel
        };

        _db.UserPlants.Add(plant);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPlant), new { id = plant.Id }, ToDto(plant));
    }

    // ── PUT api/garden/{id} ───────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserPlantDto>> UpdatePlant(int id, [FromBody] UpdateUserPlantDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var plant = await _db.UserPlants
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plant is null) return NotFound();

        // Notes and reminders are editable for all plants
        plant.Notes = dto.Notes;
        plant.WateringReminderEnabled = dto.WateringReminderEnabled;
        plant.WateringFrequencyDays = dto.WateringFrequencyDays;
        plant.LastWateredAt = dto.LastWateredAt;

        // Name and care attributes are only editable for custom plants
        if (plant.PlantId == 0)
        {
            if (!string.IsNullOrWhiteSpace(dto.CommonName))
                plant.CommonName = dto.CommonName;
            plant.ScientificName = dto.ScientificName ?? string.Empty;
            plant.Watering = dto.Watering;
            plant.Sunlight = dto.Sunlight;
            plant.Cycle = dto.Cycle;
            plant.CareLevel = dto.CareLevel;
        }

        await _db.SaveChangesAsync();

        return Ok(ToDto(plant));
    }

    // ── DELETE api/garden/{id} ────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemovePlant(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var plant = await _db.UserPlants
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plant is null) return NotFound();

        _db.UserPlants.Remove(plant);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── PUT api/garden/{id}/watered ───────────────────────────────────────────
    /// <summary>Records that a plant was watered right now.</summary>
    [HttpPut("{id:int}/watered")]
    public async Task<ActionResult<UserPlantDto>> MarkWatered(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var plant = await _db.UserPlants
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plant is null) return NotFound();

        plant.LastWateredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToDto(plant));
    }
}

