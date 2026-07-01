namespace LovePlaceApp.Dtos.Geocoding;

public class ReverseGeocodeResponseDto
{
    public string PlaceName { get; set; } = string.Empty;
    public string? Address { get; set; }
}
