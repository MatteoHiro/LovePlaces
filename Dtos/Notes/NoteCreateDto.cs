using System.ComponentModel.DataAnnotations;

namespace LovePlaceApp.Dtos.Notes;

public class NoteCreateDto
{
    [Required, MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    public int Visibility { get; set; } = 0;
}
