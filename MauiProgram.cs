using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using LovePlaceApp.Pages;
using LovePlaceApp.Services;
using LovePlaceApp.ViewModels;
using Microsoft.Extensions.Options;

namespace LovePlaceApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		var environmentName = builder.Environment.EnvironmentName;

#if DEBUG
		environmentName = Environments.Development;
#endif

		builder.Configuration
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if !WINDOWS
		builder.UseMauiMaps();
#endif

		builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));

		builder.Services.AddSingleton<IAuthStateService, AuthStateService>();
		builder.Services.AddSingleton<IGeoLocationService, DeviceGeoLocationService>();

		builder.Services.AddHttpClient<IApiService, ApiService>((serviceProvider, client) =>
		{
			var options = serviceProvider.GetRequiredService<IOptions<ApiOptions>>().Value;
			client.BaseAddress = new Uri(options.BaseUrl);
		});

		builder.Services.AddSingleton<AppShell>();

		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<ConnectionsListViewModel>();
		builder.Services.AddTransient<ConnectionDetailViewModel>();
		builder.Services.AddTransient<PlaceListViewModel>();
		builder.Services.AddTransient<AddPlaceViewModel>();
		builder.Services.AddTransient<PlaceDetailViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<ConnectionsListPage>();
		builder.Services.AddTransient<ConnectionDetailPage>();
		builder.Services.AddTransient<PlaceListPage>();
		builder.Services.AddTransient<AddPlacePage>();
		builder.Services.AddTransient<PlaceDetailPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
