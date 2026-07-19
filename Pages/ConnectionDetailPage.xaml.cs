using System.Collections.Specialized;
using LovePlaceApp.Models;
using LovePlaceApp.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace LovePlaceApp.Pages;

[QueryProperty(nameof(ConnectionId), "connectionId")]
public partial class ConnectionDetailPage : ContentPage
{
    private readonly ConnectionDetailViewModel _viewModel;

    public string? ConnectionId { get; set; }

    private void OnConnectionIdChanged()
    {
        if (int.TryParse(ConnectionId, out var id))
        {
            _viewModel.SetConnection(id);
        }
    }

    public ConnectionDetailPage(ConnectionDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PlacePins.CollectionChanged += OnPinsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        RefreshMapPins();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ConnectionMap.Pins.Clear();
    }

    private void OnPinsChanged(object? sender, NotifyCollectionChangedEventArgs e) // NOSONAR: accesses instance method RefreshMapPins
    {
        MainThread.BeginInvokeOnMainThread(RefreshMapPins);
    }

    private void RefreshMapPins()
    {
        ConnectionMap.Pins.Clear();

        foreach (var pinModel in _viewModel.PlacePins)
        {
            ConnectionMap.Pins.Add(ToPin(pinModel));
        }

        var firstPin = _viewModel.PlacePins.FirstOrDefault();
        if (firstPin is null)
        {
            return;
        }

        ConnectionMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(firstPin.Latitude, firstPin.Longitude),
            Distance.FromKilometers(1.5)));
    }

    private static Pin ToPin(MapPlacePin pinModel)
    {
        return new Pin
        {
            Label = pinModel.Label,
            Address = pinModel.Address ?? string.Empty,
            Location = new Location(pinModel.Latitude, pinModel.Longitude)
        };
    }
}
