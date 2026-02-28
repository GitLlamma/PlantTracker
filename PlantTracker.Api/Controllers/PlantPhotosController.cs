using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantTracker.Api.Data;
using PlantTracker.Api.Models;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.Api.Controllers;

[ApiController]
[Route("api/garden/{userPlantId:int}/photos")]
[Authorize]
public class PlantPhotosController : ControllerBase
{
    private readonly AppDbContext _db;

    // Max ~5 MB per photo (base64 is ~4/3 the size of raw bytes)
    private const int MaxImageBytes = 5 * 1024 * 1024;

    public PlantPhotosController(AppDbContext db)
    {
        _db = db;
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private static PlantPhotoDto ToDto(PlantPhoto p) => new()
    {
        Id = p.Id,
        UserPlantId = p.UserPlantId,
        ImageData = p.ImageData,
        Caption = p.Caption,
        TakenAt = p.TakenAt
    };

    // GET api/garden/{userPlantId}/photos
    [HttpGet]
    public async Task<ActionResult<List<PlantPhotoDto>>> GetPhotos(int userPlantId)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        // Verify the plant belongs to the user
        var plantExists = await _db.UserPlants
            .AnyAsync(p => p.Id == userPlantId && p.UserId == userId);
        if (!plantExists) return NotFound();

        var photos = await _db.PlantPhotos
            .Where(p => p.UserPlantId == userPlantId && p.UserId == userId)
            .OrderByDescending(p => p.TakenAt)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(photos);
    }

    // POST api/garden/{userPlantId}/photos
    [HttpPost]
    public async Task<ActionResult<PlantPhotoDto>> AddPhoto(int userPlantId, [FromBody] PlantPhotoDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var plantExists = await _db.UserPlants
            .AnyAsync(p => p.Id == userPlantId && p.UserId == userId);
        if (!plantExists) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.ImageData))
            return BadRequest("Image data is required.");

        // Rough size check on the base64 string
        if (dto.ImageData.Length > MaxImageBytes * 2)
            return BadRequest("Image is too large. Maximum size is 5 MB.");

        var photo = new PlantPhoto
        {
            UserPlantId = userPlantId,
            UserId = userId,
            ImageData = dto.ImageData,
            Caption = dto.Caption,
            TakenAt = DateTime.UtcNow
        };

        _db.PlantPhotos.Add(photo);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPhotos), new { userPlantId }, ToDto(photo));
    }

    // DELETE api/garden/{userPlantId}/photos/{photoId}
    [HttpDelete("{photoId:int}")]
    public async Task<IActionResult> DeletePhoto(int userPlantId, int photoId)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var photo = await _db.PlantPhotos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.UserPlantId == userPlantId && p.UserId == userId);

        if (photo is null) return NotFound();

        _db.PlantPhotos.Remove(photo);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

