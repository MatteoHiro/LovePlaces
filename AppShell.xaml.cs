using LovePlaceApp.Pages;
using LovePlaceApp.Services;

namespace LovePlaceApp;

public partial class AppShell : Shell
{
	private readonly IAuthStateService _authStateService;

	public AppShell(IAuthStateService authStateService)
	{
		InitializeComponent();
		_authStateService = authStateService;

		Routing.RegisterRoute("register", typeof(RegisterPage));
		Routing.RegisterRoute("settings", typeof(SettingsPage));
		Routing.RegisterRoute("connection-detail", typeof(ConnectionDetailPage));
		Routing.RegisterRoute("places", typeof(PlaceListPage));
		Routing.RegisterRoute("add-place", typeof(AddPlacePage));
		Routing.RegisterRoute("place-detail", typeof(PlaceDetailPage));
	}

	public async Task InitializeAsync()
	{
		try
		{
			await _authStateService.LoadTokenAsync();
			await GoToAsync(_authStateService.IsLoggedIn ? "//connections" : "//login");
		}
		catch
		{
			await GoToAsync("//login");
		}
	}
}
