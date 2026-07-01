using LovePlaceApp.Dtos.Auth;
using LovePlaceApp.Dtos.Connections;
using LovePlaceApp.Dtos.Notes;
using LovePlaceApp.Dtos.Photos;
using LovePlaceApp.Dtos.Places;

namespace LovePlaceApp.Services;

public interface IApiService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<LoginResponseDto?> RegisterAsync(RegisterRequestDto dto);

    Task<List<ConnectionResponseDto>> GetConnectionsAsync();
    Task<InvitationResponseDto?> GenerateInviteAsync();
    Task<ConnectionResponseDto?> AcceptInviteAsync(string code);
    Task DeleteConnectionAsync(int connectionId);

    Task<List<PlaceResponseDto>> GetPlacesAsync(int connectionId);
    Task<PlaceResponseDto?> GetPlaceAsync(int connectionId, int placeId);
    Task<PlaceResponseDto?> CreatePlaceAsync(int connectionId, PlaceCreateDto dto);
    Task<PlaceResponseDto?> UpdatePlaceAsync(int connectionId, int placeId, PlaceCreateDto dto);
    Task DeletePlaceAsync(int connectionId, int placeId);

    Task<List<NoteResponseDto>> GetNotesAsync(int connectionId, int placeId);
    Task<NoteResponseDto?> AddNoteAsync(int connectionId, int placeId, NoteCreateDto dto);
    Task DeleteNoteAsync(int connectionId, int noteId);

    Task<List<PhotoResponseDto>> GetPhotosAsync(int connectionId, int placeId);
    Task<PhotoResponseDto?> UploadPhotoAsync(int connectionId, int placeId, Stream fileStream, string fileName, string contentType, string? caption);
    Task DeletePhotoAsync(int connectionId, int photoId);

    Task<(string PlaceName, string? Address)?> ReverseGeocodeAsync(double lat, double lng);
    Task<PlaceTagOptionsDto?> GetTagOptionsAsync();
}
