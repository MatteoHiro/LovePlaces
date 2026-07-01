namespace LovePlaceApp.Dtos.Connections;

public class ConnectionResponseDto
{
    public int Id { get; set; }
    public string PartnerUserId { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string? PartnerAvatarUrl { get; set; }
    public int PlaceCount { get; set; }
    public DateTime ConnectedSince { get; set; }
}
