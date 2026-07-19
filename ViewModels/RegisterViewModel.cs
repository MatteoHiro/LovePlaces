using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Dtos.Auth;
using LovePlaceApp.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace LovePlaceApp.ViewModels;

public partial class RegisterViewModel(IApiService apiService, IAuthStateService authStateService) : ObservableObject
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9._-]{3,30}$", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string password = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string age = string.Empty;

    [ObservableProperty]
    private string gender = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? helperMessage = "Password: min 8 caratteri, almeno una cifra, una maiuscola e una minuscola.";

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
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
            var response = await apiService.RegisterAsync(new RegisterRequestDto
            {
                Email = Email.Trim(),
                Username = Username.Trim(),
                Password = Password,
                DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim(),
                Age = int.TryParse(Age, out var parsedAge) ? parsedAge : null,
                Gender = string.IsNullOrWhiteSpace(Gender) ? null : Gender.Trim()
            });

            if (response is null || string.IsNullOrWhiteSpace(response.Token))
            {
                ErrorMessage = "Registrazione non riuscita. Verifica i dati o prova con un'altra email.";
                return;
            }

            await authStateService.SaveTokenAsync(response.Token);
            await Shell.Current.GoToAsync("//connections");
        }
        catch (HttpRequestException)
        {
#if DEBUG
            var localToken = CreateLocalDevToken(Email, Username);
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
            ErrorMessage = "Errore durante la registrazione.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }

    private bool CanRegister() // NOSONAR: accesses observable instance properties IsBusy, Email, Username, Password, ConfirmPassword
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(Email)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password)
            && !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private bool ValidateInputs() // NOSONAR: accesses and sets observable instance properties
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email, username e password sono obbligatori.";
            return false;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = "Inserisci un'email valida.";
            return false;
        }

        if (!UsernameRegex.IsMatch(Username.Trim()))
        {
            ErrorMessage = "Username non valido. Usa 3-30 caratteri: lettere, numeri, punto, underscore o trattino.";
            return false;
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "La password deve avere almeno 8 caratteri.";
            return false;
        }

        if (!Password.Any(char.IsDigit))
        {
            ErrorMessage = "La password deve contenere almeno una cifra.";
            return false;
        }

        if (!Password.Any(char.IsUpper) || !Password.Any(char.IsLower))
        {
            ErrorMessage = "La password deve contenere almeno una lettera maiuscola e una minuscola.";
            return false;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Le password non coincidono.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Age) && (!int.TryParse(Age, out var parsedAge) || parsedAge <= 0 || parsedAge > 120))
        {
            ErrorMessage = "Eta non valida.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    partial void OnEmailChanged(string value) => ErrorMessage = null;
    partial void OnUsernameChanged(string value) => ErrorMessage = null;
    partial void OnPasswordChanged(string value) => ErrorMessage = null;
    partial void OnConfirmPasswordChanged(string value) => ErrorMessage = null;
    partial void OnAgeChanged(string value) => ErrorMessage = null;

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

    private static string CreateLocalDevToken(string email, string username)
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "LovePlaceApp.LocalDev",
            audience: "LovePlaceApp.LocalDev",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email.Trim()),
                new Claim("name", username.Trim())
            ],
            notBefore: now,
            expires: now.AddDays(30));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
