using CommunityToolkit.Mvvm.ComponentModel;

namespace LovePlaceApp.Models;

public partial class SelectableTag(string value) : ObservableObject
{
    public string Value { get; } = value;

    [ObservableProperty]
    private bool isSelected;
}
