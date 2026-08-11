using System.Collections.ObjectModel;
using System.Linq;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace AMEFManager.ViewModels.Windows;
public partial class BackupWindowViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly BackupService _backupService;
    [ObservableProperty]
    private int _backupIntervalDays;
    [ObservableProperty]
    private int _backupsToKeep;
    [ObservableProperty]
    private string? _selectedBackup;
    public ObservableCollection<string> AvailableBackups { get; }
    public BackupWindowViewModel(SettingsService settingsService, BackupService backupService)
    {
        _settingsService = settingsService;
        _backupService = backupService;
        var settings = _settingsService.GetSettings();
        BackupIntervalDays = settings.BackupIntervalDays;
        BackupsToKeep = settings.BackupsToKeep;
        AvailableBackups = new ObservableCollection<string>(_backupService.GetAvailableBackups());
        SelectedBackup = AvailableBackups.FirstOrDefault();
    }
    [RelayCommand]
    private void Save()
    {
        var settings = _settingsService.GetSettings();
        settings.BackupIntervalDays = BackupIntervalDays;
        settings.BackupsToKeep = BackupsToKeep;
        _settingsService.SaveSettings(settings);
    }
    [RelayCommand]
    private void CreateBackupNow()
    {
        if (_backupService.PerformBackup())
        {
            RefreshBackups();
        }
    }
    [RelayCommand]
    private void Restore()
    {
        if (!string.IsNullOrEmpty(SelectedBackup))
        {
            if (_backupService.RestoreBackup(SelectedBackup))
            {
                System.Environment.Exit(0);
            }
        }
    }
    private void RefreshBackups()
    {
        AvailableBackups.Clear();
        foreach (var backup in _backupService.GetAvailableBackups())
        {
            AvailableBackups.Add(backup);
        }
        SelectedBackup = AvailableBackups.FirstOrDefault();
    }
}
