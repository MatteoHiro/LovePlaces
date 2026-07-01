using System.ComponentModel.DataAnnotations;

namespace LovePlaceApp.Dtos.Places;

public class PlaceCreateDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Required]
    public DateOnly DateVisited { get; set; }

    [MaxLength(50)]
    public string PlaceType { get; set; } = "Altro";

    public List<string> MusicVibes { get; set; } = [];
    public List<string> AtmosphereTags { get; set; } = [];
    public List<string> CuisineTags { get; set; } = [];

    [Range(1, 5)]
    public int Atmosphere { get; set; }

    [Range(1, 5)]
    public int ValueForMoney { get; set; }
}
