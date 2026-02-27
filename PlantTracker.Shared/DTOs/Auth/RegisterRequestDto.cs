using System.ComponentModel.DataAnnotations;

namespace PlantTracker.Shared.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Zip code must be 5 digits.")]
    public string ZipCode { get; set; } = string.Empty;
}

