using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LovePlaceApp.Dtos.Auth;
using LovePlaceApp.Dtos.Connections;
using LovePlaceApp.Dtos.Geocoding;
using LovePlaceApp.Dtos.Notes;
using LovePlaceApp.Dtos.Photos;
using LovePlaceApp.Dtos.Places;

namespace LovePlaceApp.Services;

public class ApiService(HttpClient httpClient, IAuthStateService authStateService) : IApiService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        return await PostAsync<LoginRequestDto, LoginResponseDto>("api/auth/login", dto, includeAuth: false);
    }

    public async Task<LoginResponseDto?> RegisterAsync(RegisterRequestDto dto)
    {
        return await PostAsync<RegisterRequestDto, LoginResponseDto>("api/auth/register", dto, includeAuth: false);
    }

    public async Task<List<ConnectionResponseDto>> GetConnectionsAsync()
    {
        return await GetAsync<List<ConnectionResponseDto>>("api/connections") ?? [];
    }

    public async Task<InvitationResponseDto?> GenerateInviteAsync()
    {
        return await PostAsync<object, InvitationResponseDto>("api/connections/invite", new { });
    }

    public async Task<ConnectionResponseDto?> AcceptInviteAsync(string code)
    {
        return await PostAsync<object, ConnectionResponseDto>($"api/connections/accept/{Uri.EscapeDataString(code)}", new { });
    }

    public async Task DeleteConnectionAsync(int connectionId)
    {
        await SendAsync(HttpMethod.Delete, $"api/connections/{connectionId}");
    }

    public async Task<List<PlaceResponseDto>> GetPlacesAsync(int connectionId)
    {
        return await GetAsync<List<PlaceResponseDto>>($"api/connections/{connectionId}/places") ?? [];
    }

    public async Task<PlaceResponseDto?> GetPlaceAsync(int connectionId, int placeId)
    {
        return await GetAsync<PlaceResponseDto>($"api/connections/{connectionId}/places/{placeId}");
    }

    public async Task<PlaceResponseDto?> CreatePlaceAsync(int connectionId, PlaceCreateDto dto)
    {
        return await PostAsync<PlaceCreateDto, PlaceResponseDto>($"api/connections/{connectionId}/places", dto);
    }

    public async Task<PlaceResponseDto?> UpdatePlaceAsync(int connectionId, int placeId, PlaceCreateDto dto)
    {
        return await PutAsync<PlaceCreateDto, PlaceResponseDto>($"api/connections/{connectionId}/places/{placeId}", dto);
    }

    public async Task DeletePlaceAsync(int connectionId, int placeId)
    {
        await SendAsync(HttpMethod.Delete, $"api/connections/{connectionId}/places/{placeId}");
    }

    public async Task<List<NoteResponseDto>> GetNotesAsync(int connectionId, int placeId)
    {
        return await GetAsync<List<NoteResponseDto>>($"api/connections/{connectionId}/places/{placeId}/notes") ?? [];
    }

    public async Task<NoteResponseDto?> AddNoteAsync(int connectionId, int placeId, NoteCreateDto dto)
    {
        return await PostAsync<NoteCreateDto, NoteResponseDto>($"api/connections/{connectionId}/places/{placeId}/notes", dto);
    }

    public async Task DeleteNoteAsync(int connectionId, int noteId)
    {
        await SendAsync(HttpMethod.Delete, $"api/connections/{connectionId}/notes/{noteId}");
    }

    public async Task<List<PhotoResponseDto>> GetPhotosAsync(int connectionId, int placeId)
    {
        return await GetAsync<List<PhotoResponseDto>>($"api/connections/{connectionId}/places/{placeId}/photos") ?? [];
    }

    public async Task<PhotoResponseDto?> UploadPhotoAsync(int connectionId, int placeId, Stream fileStream, string fileName, string contentType, string? caption)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);

        if (!string.IsNullOrWhiteSpace(caption))
        {
            form.Add(new StringContent(caption), "caption");
        }

        var message = new HttpRequestMessage(HttpMethod.Post, $"api/connections/{connectionId}/places/{placeId}/photos")
        {
            Content = form
        };

        ApplyAuthorization(message, includeAuth: true);
        var response = await httpClient.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<PhotoResponseDto>(_jsonOptions);
    }

    public async Task DeletePhotoAsync(int connectionId, int photoId)
    {
        await SendAsync(HttpMethod.Delete, $"api/connections/{connectionId}/photos/{photoId}");
    }

    public async Task<(string PlaceName, string? Address)?> ReverseGeocodeAsync(double lat, double lng)
    {
        var result = await PostAsync<ReverseGeocodeRequestDto, ReverseGeocodeResponseDto>(
            "api/geocoding/reverse",
            new ReverseGeocodeRequestDto { Lat = lat, Lng = lng });

        return result is null ? null : (result.PlaceName, result.Address);
    }

    public async Task<PlaceTagOptionsDto?> GetTagOptionsAsync()
    {
        return await GetAsync<PlaceTagOptionsDto>("api/tags");
    }

    private async Task<TResponse?> GetAsync<TResponse>(string path)
    {
        var response = await SendAsync(HttpMethod.Get, path);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest request, bool includeAuth = true)
    {
        var response = await SendAsync(HttpMethod.Post, path, request, includeAuth);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    private async Task<TResponse?> PutAsync<TRequest, TResponse>(string path, TRequest request)
    {
        var response = await SendAsync(HttpMethod.Put, path, request);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    private async Task<HttpResponseMessage> SendAsync<TRequest>(HttpMethod method, string path, TRequest request, bool includeAuth = true)
    {
        var message = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        ApplyAuthorization(message, includeAuth);
        return await httpClient.SendAsync(message);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, bool includeAuth = true)
    {
        var message = new HttpRequestMessage(method, path);
        ApplyAuthorization(message, includeAuth);
        return await httpClient.SendAsync(message);
    }

    private void ApplyAuthorization(HttpRequestMessage message, bool includeAuth)
    {
        if (!includeAuth || string.IsNullOrWhiteSpace(authStateService.Token))
        {
            return;
        }

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authStateService.Token);
    }
}
