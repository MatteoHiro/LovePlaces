using System.ComponentModel.DataAnnotations;

namespace LovePlaceApp.Dtos.Auth;

public class RegisterRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
}
