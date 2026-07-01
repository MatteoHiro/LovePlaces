using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Models;
using LovePlaceApp.Services;

namespace LovePlaceApp.ViewModels;

public partial class ConnectionDetailViewModel(IApiService apiService) : ObservableObject
{
    public ObservableCollection<MapPlacePin> PlacePins { get; } = [];

    [ObservableProperty]
    private int connectionId;

    [ObservableProperty]
    private string title = "Mondo della connessione";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    public void SetConnection(int id)
    {
        ConnectionId = id;
        Title = $"Connessione #{id}";
    }

    [RelayCommand]
    private async Task LoadAsync()
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
            PlacePins.Clear();

            foreach (var place in places.Where(p => p.Latitude.HasValue && p.Longitude.HasValue))
            {
                PlacePins.Add(new MapPlacePin
                {
                    PlaceId = place.Id,
                    Label = place.Name,
                    Address = place.Address,
                    Latitude = place.Latitude!.Value,
                    Longitude = place.Longitude!.Value
                });
            }

            if (PlacePins.Count == 0)
            {
                StatusMessage = "Nessun posto geolocalizzato in questa connessione.";
            }
        }
        catch
        {
            StatusMessage = "Errore durante il caricamento della mappa.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenPlacesAsync()
    {
        return Shell.Current.GoToAsync($"places?connectionId={ConnectionId}");
    }

    [RelayCommand]
    private Task AddPlaceAsync()
    {
        return Shell.Current.GoToAsync($"add-place?connectionId={ConnectionId}");
    }
}
