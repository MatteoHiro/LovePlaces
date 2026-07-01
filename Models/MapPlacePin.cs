namespace LovePlaceApp.Models;

public class MapPlacePin
{
    public int PlaceId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
