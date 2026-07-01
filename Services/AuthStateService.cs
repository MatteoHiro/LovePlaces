using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LovePlaceApp.Services;

public class AuthStateService : IAuthStateService
{
    private const string TokenKey = "jwt_token";

    public string? Token { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);
    public Guid CurrentUserId { get; private set; }

    public async Task SaveTokenAsync(string token)
    {
        Token = token;
        CurrentUserId = ExtractUserId(token);

        try
        {
            await SecureStorage.SetAsync(TokenKey, token);
        }
        catch
        {
            // Keep in-memory auth state even if secure storage is unavailable on this platform/runtime.
        }
    }

    public async Task LoadTokenAsync()
    {
        try
        {
            Token = await SecureStorage.GetAsync(TokenKey);
        }
        catch
        {
            Token = null;
        }

        CurrentUserId = string.IsNullOrWhiteSpace(Token) ? Guid.Empty : ExtractUserId(Token);
    }

    public Task ClearTokenAsync()
    {
        Token = null;
        CurrentUserId = Guid.Empty;

        try
        {
            SecureStorage.Remove(TokenKey);
        }
        catch
        {
            // Ignore storage cleanup errors to keep logout flow resilient.
        }

        return Task.CompletedTask;
    }

    private static Guid ExtractUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Accept both standard sub and NameIdentifier conventions.
        var rawId = jwt.Subject
            ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? jwt.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;

        return Guid.TryParse(rawId, out var userId) ? userId : Guid.Empty;
    }
}
