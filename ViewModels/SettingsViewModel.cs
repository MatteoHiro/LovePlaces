using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LovePlaceApp.Services;

namespace LovePlaceApp.ViewModels;

public partial class SettingsViewModel(IAuthStateService authStateService) : ObservableObject
{
    [RelayCommand]
    private async Task LogoutAsync()
    {
        await authStateService.ClearTokenAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
