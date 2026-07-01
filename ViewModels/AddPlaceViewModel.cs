using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Dtos.Places;
using LovePlaceApp.Models;
using LovePlaceApp.Services;

namespace LovePlaceApp.ViewModels;

public partial class AddPlaceViewModel(IApiService apiService, IGeoLocationService geoLocationService) : ObservableObject
{
    [ObservableProperty]
    private int connectionId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? address;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private double? latitude;

    [ObservableProperty]
    private double? longitude;

    [ObservableProperty]
    private string placeType = "Altro";

    [ObservableProperty]
    private int atmosphere = 3;

    [ObservableProperty]
    private int valueForMoney = 3;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private List<string> placeTypeOptions = [];

    public ObservableCollection<SelectableTag> MusicVibeOptions { get; } = [];
    public ObservableCollection<SelectableTag> AtmosphereTagOptions { get; } = [];
    public ObservableCollection<SelectableTag> CuisineTagOptions { get; } = [];

    public bool IsFoodPlace => PlaceTagOptions.FoodPlaceTypes.Contains(PlaceType);

    public void SetConnection(int id)
    {
        ConnectionId = id;
    }

    public async Task InitializeAsync()
    {
        if (PlaceTypeOptions.Count > 0)
        {
            return;
        }

        var tags = await apiService.GetTagOptionsAsync();
        var placeTypes = tags?.PlaceTypes?.Count > 0 ? tags.PlaceTypes : [.. PlaceTagOptions.PlaceTypes];
        var music = tags?.MusicVibes?.Count > 0 ? tags.MusicVibes : [.. PlaceTagOptions.MusicVibes];
        var atmosphere = tags?.AtmosphereTags?.Count > 0 ? tags.AtmosphereTags : [.. PlaceTagOptions.AtmosphereTags];
        var cuisine = tags?.CuisineTags?.Count > 0 ? tags.CuisineTags : [.. PlaceTagOptions.CuisineTags];

        PlaceTypeOptions = placeTypes;
        PlaceType = PlaceTypeOptions.FirstOrDefault() ?? "Altro";

        PopulateSelectable(MusicVibeOptions, music);
        PopulateSelectable(AtmosphereTagOptions, atmosphere);
        PopulateSelectable(CuisineTagOptions, cuisine);
    }

    [RelayCommand]
    private async Task DetectLocationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Rilevamento posizione...";

        try
        {
            var location = await geoLocationService.GetCurrentLocationAsync();
            if (location is null)
            {
                StatusMessage = "Posizione non disponibile.";
                return;
            }

            Latitude = location.Value.Lat;
            Longitude = location.Value.Lng;

            StatusMessage = "Geocoding...";
            var result = await apiService.ReverseGeocodeAsync(location.Value.Lat, location.Value.Lng);
            if (result.HasValue)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    Name = result.Value.PlaceName;
                }

                Address = result.Value.Address;
            }

            StatusMessage = null;
        }
        catch
        {
            StatusMessage = "Impossibile rilevare la posizione.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy || ConnectionId <= 0 || string.IsNullOrWhiteSpace(Name))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var dto = new PlaceCreateDto
            {
                Name = Name.Trim(),
                Address = Address,
                Description = Description,
                Latitude = Latitude,
                Longitude = Longitude,
                DateVisited = DateOnly.FromDateTime(DateTime.Today),
                PlaceType = PlaceType,
                MusicVibes = [.. MusicVibeOptions.Where(t => t.IsSelected).Select(t => t.Value)],
                AtmosphereTags = [.. AtmosphereTagOptions.Where(t => t.IsSelected).Select(t => t.Value)],
                CuisineTags = IsFoodPlace ? [.. CuisineTagOptions.Where(t => t.IsSelected).Select(t => t.Value)] : [],
                Atmosphere = Atmosphere,
                ValueForMoney = ValueForMoney
            };

            var created = await apiService.CreatePlaceAsync(ConnectionId, dto);
            if (created is null)
            {
                StatusMessage = "Salvataggio non riuscito.";
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            StatusMessage = "Errore durante il salvataggio.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnPlaceTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsFoodPlace));
    }

    private static void PopulateSelectable(ObservableCollection<SelectableTag> target, IEnumerable<string> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(new SelectableTag(value));
        }
    }
}
