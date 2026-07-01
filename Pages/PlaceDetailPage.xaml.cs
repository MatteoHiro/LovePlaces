using LovePlaceApp.ViewModels;

namespace LovePlaceApp.Pages;

[QueryProperty(nameof(ConnectionId), "connectionId")]
[QueryProperty(nameof(PlaceId), "placeId")]
public partial class PlaceDetailPage : ContentPage
{
    private readonly PlaceDetailViewModel _viewModel;
    private int _connectionId;
    private int _placeId;

    public string ConnectionId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _connectionId = id;
                _viewModel.SetRoute(_connectionId, _placeId);
            }
        }
    }

    public string PlaceId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _placeId = id;
                _viewModel.SetRoute(_connectionId, _placeId);
            }
        }
    }

    public PlaceDetailPage(PlaceDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
