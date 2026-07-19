using LovePlaceApp.ViewModels;

namespace LovePlaceApp.Pages;

[QueryProperty(nameof(ConnectionId), "connectionId")]
public partial class AddPlacePage : ContentPage
{
    private readonly AddPlaceViewModel _viewModel;

    public string? ConnectionId { get; set; }

    private void OnConnectionIdChanged()
    {
        if (int.TryParse(ConnectionId, out var id))
        {
            _viewModel.SetConnection(id);
        }
    }

    public AddPlacePage(AddPlaceViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
