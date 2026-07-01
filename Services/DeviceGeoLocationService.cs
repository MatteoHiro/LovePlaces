namespace LovePlaceApp.Services;

public class DeviceGeoLocationService : IGeoLocationService
{
    public async Task<(double Lat, double Lng)?> GetCurrentLocationAsync()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
        var location = await Geolocation.Default.GetLocationAsync(request);

        if (location is null)
        {
            return null;
        }

        return (location.Latitude, location.Longitude);
    }
}
