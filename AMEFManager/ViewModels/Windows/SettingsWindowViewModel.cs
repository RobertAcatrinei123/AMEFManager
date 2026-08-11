using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace AMEFManager.ViewModels.Windows;
public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private AppSettings _currentSettings;
    [ObservableProperty]
    private string _serverPath = string.Empty;

    public SettingsWindowViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentSettings = _settingsService.GetSettings();
        
        ServerPath = _currentSettings.ServerPath;
    }

    [RelayCommand]
    private void Save()
    {
        _currentSettings.ServerPath = ServerPath;
        
        _settingsService.SaveSettings(_currentSettings);
    }
}
