using LovePlaceApp.ViewModels;

namespace LovePlaceApp.Pages;

[QueryProperty(nameof(ConnectionId), "connectionId")]
public partial class PlaceListPage : ContentPage
{
    private readonly PlaceListViewModel _viewModel;

    public string? ConnectionId { get; set; }

    private void OnConnectionIdChanged()
    {
        if (int.TryParse(ConnectionId, out var id))
        {
            _viewModel.SetConnection(id);
        }
    }

    public PlaceListPage(PlaceListViewModel viewModel)
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
