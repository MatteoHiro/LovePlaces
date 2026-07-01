namespace LovePlaceApp.Services;

public interface IGeoLocationService
{
    Task<(double Lat, double Lng)?> GetCurrentLocationAsync();
}
