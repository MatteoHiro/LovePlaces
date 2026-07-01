using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Dtos.Connections;
using LovePlaceApp.Services;

namespace LovePlaceApp.ViewModels;

public partial class ConnectionsListViewModel(IApiService apiService) : ObservableObject
{
    private static readonly Regex InviteCodeRegex = new("^[A-Za-z0-9_-]{4,64}$", RegexOptions.Compiled);

    public ObservableCollection<ConnectionResponseDto> Connections { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateInviteCommand))]
    [NotifyCanExecuteChangedFor(nameof(AcceptInviteCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptInviteCommand))]
    private string inviteCodeToAccept = string.Empty;

    [ObservableProperty]
    private string? generatedInviteCode;

    [ObservableProperty]
    private string? generatedInviteDeepLink;

    [ObservableProperty]
    private string? helperMessage = "Ogni connessione e un mondo isolato: usa un codice invito personale.";

    [RelayCommand(CanExecute = nameof(CanExecuteBusyActions))]
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            await RefreshConnectionsAsync();
        }
        catch (HttpRequestException)
        {
            StatusMessage = "Server non raggiungibile. Controlla la connessione e riprova.";
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Richiesta scaduta. Riprova tra qualche secondo.";
        }
        catch
        {
            StatusMessage = "Errore durante il caricamento delle connessioni.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteBusyActions))]
    private async Task GenerateInviteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var invite = await apiService.GenerateInviteAsync();

            if (invite is null || string.IsNullOrWhiteSpace(invite.Code))
            {
                StatusMessage = "Impossibile generare l'invito.";
                return;
            }

            GeneratedInviteCode = invite.Code;
            GeneratedInviteDeepLink = $"loveplaces://invite/{invite.Code}";
            StatusMessage = "Invito generato con successo.";
        }
        catch (HttpRequestException)
        {
            StatusMessage = "Server non raggiungibile. Controlla la connessione e riprova.";
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Richiesta scaduta. Riprova tra qualche secondo.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAcceptInvite))]
    private async Task AcceptInviteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var normalizedCode = NormalizeInviteCode(InviteCodeToAccept);
        if (!IsValidInviteCode(normalizedCode))
        {
            StatusMessage = "Codice invito non valido. Usa 4-64 caratteri (lettere, numeri, _ o -).";
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        var shouldRefresh = false;

        try
        {
            var accepted = await apiService.AcceptInviteAsync(normalizedCode);
            if (accepted is null)
            {
                StatusMessage = "Invito non valido o scaduto.";
                return;
            }

            InviteCodeToAccept = string.Empty;
            StatusMessage = "Connessione creata con successo.";
            shouldRefresh = true;
        }
        catch (HttpRequestException)
        {
            StatusMessage = "Server non raggiungibile. Controlla la connessione e riprova.";
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Richiesta scaduta. Riprova tra qualche secondo.";
        }
        finally
        {
            IsBusy = false;
        }

        if (shouldRefresh)
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private Task GoToSettingsAsync()
    {
        return Shell.Current.GoToAsync("settings");
    }

    [RelayCommand]
    private Task OpenConnectionAsync(int connectionId)
    {
        if (connectionId <= 0)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync($"connection-detail?connectionId={connectionId}");
    }

    private bool CanExecuteBusyActions()
    {
        return !IsBusy;
    }

    private bool CanAcceptInvite()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(InviteCodeToAccept);
    }

    partial void OnInviteCodeToAcceptChanged(string value)
    {
        StatusMessage = null;
    }

    private async Task RefreshConnectionsAsync()
    {
        var list = await apiService.GetConnectionsAsync();
        Connections.Clear();

        foreach (var connection in list)
        {
            Connections.Add(connection);
        }

        if (Connections.Count == 0)
        {
            StatusMessage = "Nessuna connessione trovata.";
        }
    }

    private static string NormalizeInviteCode(string value)
    {
        return value.Trim();
    }

    private static bool IsValidInviteCode(string value)
    {
        return InviteCodeRegex.IsMatch(value);
    }
}
