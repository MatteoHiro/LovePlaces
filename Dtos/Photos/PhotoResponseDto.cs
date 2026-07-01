namespace LovePlaceApp.Dtos.Photos;

public class PhotoResponseDto
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? AuthorName { get; set; }
    public bool IsOwn { get; set; }
}
