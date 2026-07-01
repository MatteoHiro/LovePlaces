namespace LovePlaceApp.Dtos.Places;

public class PlaceResponseDto
{
    public int Id { get; set; }
    public int ConnectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateOnly DateVisited { get; set; }
    public string PlaceType { get; set; } = string.Empty;
    public List<string> MusicVibes { get; set; } = [];
    public List<string> AtmosphereTags { get; set; } = [];
    public List<string> CuisineTags { get; set; } = [];
    public int Atmosphere { get; set; }
    public int ValueForMoney { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public string AuthorName { get; set; } = string.Empty;
    public bool IsAuthor { get; set; }
    public DateTime CreatedAt { get; set; }
}
