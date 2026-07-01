namespace LovePlaceApp.Dtos.Connections;

public class InvitationResponseDto
{
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
