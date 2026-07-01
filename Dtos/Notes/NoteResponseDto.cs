namespace LovePlaceApp.Dtos.Notes;

public class NoteResponseDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Visibility { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public bool IsOwn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
