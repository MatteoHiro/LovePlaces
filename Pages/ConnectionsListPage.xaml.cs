using LovePlaceApp.ViewModels;

namespace LovePlaceApp.Pages;

public partial class ConnectionsListPage : ContentPage
{
    private readonly ConnectionsListViewModel _viewModel;

    public ConnectionsListPage(ConnectionsListViewModel viewModel)
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
