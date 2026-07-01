using LovePlaceApp.Services;

namespace LovePlaceApp;

public partial class App : Application
{
	private readonly AppShell _appShell;

	public App(AppShell appShell)
	{
		InitializeComponent();
		_appShell = appShell;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				await _appShell.InitializeAsync();
			}
			catch
			{
				await _appShell.GoToAsync("//login");
			}
		});

		return new Window(_appShell);
	}
}