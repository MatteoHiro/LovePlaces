using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Dtos.Notes;
using LovePlaceApp.Dtos.Photos;
using LovePlaceApp.Dtos.Places;
using LovePlaceApp.Services;

namespace LovePlaceApp.ViewModels;

public partial class PlaceDetailViewModel(IApiService apiService) : ObservableObject
{
    public ObservableCollection<NoteResponseDto> Notes { get; } = [];
    public ObservableCollection<PhotoResponseDto> Photos { get; } = [];

    [ObservableProperty]
    private int connectionId;

    [ObservableProperty]
    private int placeId;

    [ObservableProperty]
    private PlaceResponseDto? place;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string newNoteText = string.Empty;

    [ObservableProperty]
    private bool isSharedNote = true;

    [ObservableProperty]
    private string? newPhotoCaption;

    public void SetRoute(int connection, int place)
    {
        ConnectionId = connection;
        PlaceId = place;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || ConnectionId <= 0 || PlaceId <= 0)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            Place = await apiService.GetPlaceAsync(ConnectionId, PlaceId);

            var notes = await apiService.GetNotesAsync(ConnectionId, PlaceId);
            Notes.Clear();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }

            var photos = await apiService.GetPhotosAsync(ConnectionId, PlaceId);
            Photos.Clear();
            foreach (var photo in photos)
            {
                Photos.Add(photo);
            }

            if (Place is null)
            {
                StatusMessage = "Posto non trovato.";
            }
        }
        catch
        {
            StatusMessage = "Errore durante il caricamento del dettaglio.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(NewNoteText) || ConnectionId <= 0 || PlaceId <= 0)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var added = await apiService.AddNoteAsync(ConnectionId, PlaceId, new NoteCreateDto
            {
                Text = NewNoteText.Trim(),
                Visibility = IsSharedNote ? 1 : 0
            });

            if (added is not null)
            {
                Notes.Insert(0, added);
                NewNoteText = string.Empty;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(int noteId)
    {
        if (IsBusy || noteId <= 0)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await apiService.DeleteNoteAsync(ConnectionId, noteId);
            var note = Notes.FirstOrDefault(n => n.Id == noteId);
            if (note is not null)
            {
                Notes.Remove(note);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (IsBusy || ConnectionId <= 0 || PlaceId <= 0)
        {
            return;
        }

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleziona una foto",
                FileTypes = FilePickerFileType.Images
            });

            if (file is null)
            {
                return;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(contentType))
            {
                StatusMessage = "Formato non supportato. Usa JPEG, PNG o WEBP.";
                return;
            }

            await using var stream = await file.OpenReadAsync();
            if (stream.CanSeek && stream.Length > 5 * 1024 * 1024)
            {
                StatusMessage = "Foto troppo grande. Max 5MB.";
                return;
            }

            IsBusy = true;
            var uploaded = await apiService.UploadPhotoAsync(ConnectionId, PlaceId, stream, file.FileName, contentType, NewPhotoCaption);
            if (uploaded is null)
            {
                StatusMessage = "Upload non riuscito.";
                return;
            }

            Photos.Insert(0, uploaded);
            NewPhotoCaption = string.Empty;
            StatusMessage = "Foto caricata con successo.";
        }
        catch
        {
            StatusMessage = "Errore durante l'upload foto.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(int photoId)
    {
        if (IsBusy || photoId <= 0)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await apiService.DeletePhotoAsync(ConnectionId, photoId);
            var photo = Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo is not null)
            {
                Photos.Remove(photo);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
