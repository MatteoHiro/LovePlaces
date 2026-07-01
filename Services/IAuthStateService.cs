namespace LovePlaceApp.Services;

public interface IAuthStateService
{
    string? Token { get; }
    bool IsLoggedIn { get; }
    Guid CurrentUserId { get; }

    Task SaveTokenAsync(string token);
    Task LoadTokenAsync();
    Task ClearTokenAsync();
}
