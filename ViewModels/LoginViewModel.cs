using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Dtos.Auth;
using LovePlaceApp.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Mail;

namespace LovePlaceApp.ViewModels;

public partial class LoginViewModel(IApiService apiService, IAuthStateService authStateService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string password = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? helperMessage = "Inserisci le credenziali usate in fase di registrazione.";

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!ValidateInputs())
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var response = await apiService.LoginAsync(new LoginRequestDto
            {
                Email = Email.Trim(),
                Password = Password
            });

            if (response is null || string.IsNullOrWhiteSpace(response.Token))
            {
                ErrorMessage = "Login non riuscito. Controlla le credenziali.";
                return;
            }

            await authStateService.SaveTokenAsync(response.Token);
            await Shell.Current.GoToAsync("//connections");
        }
        catch (HttpRequestException)
        {
#if DEBUG
            var localToken = CreateLocalDevToken(Email);
            await authStateService.SaveTokenAsync(localToken);
            HelperMessage = "Modalita locale attiva: backend non raggiungibile, accesso demo eseguito.";
            await Shell.Current.GoToAsync("//connections");
#else
            ErrorMessage = "Server non raggiungibile. Controlla la connessione e riprova.";
#endif
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Richiesta scaduta. Riprova tra qualche secondo.";
        }
        catch
        {
            ErrorMessage = "Errore durante il login.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToRegisterAsync()
    {
        return Shell.Current.GoToAsync("register");
    }

    private bool CanLogin()
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(Email)
            && !string.IsNullOrWhiteSpace(Password);
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email e password sono obbligatorie.";
            return false;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = "Inserisci un'email valida.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    partial void OnEmailChanged(string value) => ErrorMessage = null;
    partial void OnPasswordChanged(string value) => ErrorMessage = null;

    private static bool IsValidEmail(string value)
    {
        try
        {
            _ = new MailAddress(value.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string CreateLocalDevToken(string email)
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "LovePlaceApp.LocalDev",
            audience: "LovePlaceApp.LocalDev",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email.Trim())
            ],
            notBefore: now,
            expires: now.AddDays(30));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
