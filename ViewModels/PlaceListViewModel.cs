using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Dtos.Places;
using LovePlaceApp.Services;

namespace LovePlaceApp.ViewModels;

public partial class PlaceListViewModel(IApiService apiService) : ObservableObject
{
    public ObservableCollection<PlaceResponseDto> Places { get; } = [];

    [ObservableProperty]
    private int connectionId;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    public void SetConnection(int id) // NOSONAR: sets observable instance property ConnectionId
    {
        ConnectionId = id;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || ConnectionId <= 0)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var places = await apiService.GetPlacesAsync(ConnectionId);
            Places.Clear();
            foreach (var place in places)
            {
                Places.Add(place);
            }

            if (places.Count == 0)
            {
                StatusMessage = "Nessun posto in questa connessione.";
            }
        }
        catch
        {
            StatusMessage = "Errore durante il caricamento dei posti.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenPlaceAsync(int placeId)
    {
        if (placeId <= 0)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync($"place-detail?connectionId={ConnectionId}&placeId={placeId}");
    }

    [RelayCommand]
    private Task AddPlaceAsync()
    {
        return Shell.Current.GoToAsync($"add-place?connectionId={ConnectionId}");
    }
}
