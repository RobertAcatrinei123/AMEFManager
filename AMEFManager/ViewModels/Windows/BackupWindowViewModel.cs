using System;
using System.Collections.ObjectModel;
using System.Linq;
using AMEFManager.Helpers;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.Windows;

public partial class BackupWindowViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly BackupService _backupService;

    public static Action? ExitAction { get; set; }

    [ObservableProperty]
    private int _backupIntervalDays;

    [ObservableProperty]
    private int _backupsToKeep;

    [ObservableProperty]
    private string? _selectedBackup;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _lastBackupDateFormatted = string.Empty;

    [ObservableProperty]
    private string _availableBackupsCountMessage = string.Empty;

    public ObservableCollection<string> AvailableBackups { get; } = new();

    public BackupWindowViewModel(SettingsService settingsService, BackupService backupService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));

        var settings = _settingsService.GetSettings();
        BackupIntervalDays = settings.BackupIntervalDays;
        BackupsToKeep = settings.BackupsToKeep;

        RefreshBackups();
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            ErrorMessage = null;
            StatusMessage = null;
            IsBusy = true;

            var settings = _settingsService.GetSettings();
            settings.BackupIntervalDays = BackupIntervalDays;
            settings.BackupsToKeep = BackupsToKeep;
            _settingsService.SaveSettings(settings);

            IsSuccess = true;
            StatusMessage = "Setările de backup au fost salvate cu succes!";
            AppLogger.LogInfo("[BackupWindowViewModel] Backup settings saved successfully.");
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            ErrorMessage = $"Eroare la salvarea setărilor de backup: {ex.Message}";
            AppLogger.LogError($"[BackupWindowViewModel] Error saving backup settings: {ex.Message}", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CreateBackupNow()
    {
        try
        {
            ErrorMessage = null;
            StatusMessage = null;
            IsBusy = true;

            var settings = _settingsService.GetSettings();
            if (BackupsToKeep <= 0)
            {
                IsSuccess = false;
                ErrorMessage = "Pentru a crea un backup, numărul de copii păstrate trebuie să fie mai mare de 0.";
                AppLogger.LogWarning("[BackupWindowViewModel] Backup creation aborted: BackupsToKeep is 0.");
                return;
            }
            if (BackupIntervalDays <= 0)
            {
                IsSuccess = false;
                ErrorMessage = "Pentru a crea un backup, setați un interval de backup (zile) mai mare de 0.";
                AppLogger.LogWarning("[BackupWindowViewModel] Backup creation aborted: BackupIntervalDays is 0.");
                return;
            }

            settings.BackupIntervalDays = BackupIntervalDays;
            settings.BackupsToKeep = BackupsToKeep;
            _settingsService.SaveSettings(settings);

            bool result = _backupService.PerformBackup();
            if (result)
            {
                RefreshBackups();
                IsSuccess = true;
                StatusMessage = "Backup-ul bazei de date a fost creat cu succes!";
                AppLogger.LogInfo("[BackupWindowViewModel] Manual database backup created successfully.");
            }
            else
            {
                IsSuccess = false;
                ErrorMessage = "Nu s-a putut crea backup-ul bazei de date. Verificați existența fișierului de date și permisiunile folderului.";
                AppLogger.LogWarning("[BackupWindowViewModel] PerformBackup returned false.");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            ErrorMessage = $"Eroare la crearea backup-ului: {ex.Message}";
            AppLogger.LogError($"[BackupWindowViewModel] Error during manual backup creation: {ex.Message}", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Restore()
    {
        try
        {
            ErrorMessage = null;
            StatusMessage = null;

            if (string.IsNullOrWhiteSpace(SelectedBackup))
            {
                IsSuccess = false;
                ErrorMessage = "Selectați un fișier de backup pentru restaurare.";
                AppLogger.LogWarning("[BackupWindowViewModel] Restore attempted without selecting a backup file.");
                return;
            }

            IsBusy = true;
            bool result = _backupService.RestoreBackup(SelectedBackup);
            if (result)
            {
                IsSuccess = true;
                StatusMessage = "Baza de date a fost restaurată cu succes. Aplicația se va închide...";
                AppLogger.LogInfo($"[BackupWindowViewModel] Database restored from backup '{SelectedBackup}'.");

                if (ExitAction != null)
                {
                    ExitAction.Invoke();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                IsSuccess = false;
                ErrorMessage = "Eroare la restaurarea bazei de date din backup. Verificați integritatea fișierului selectat.";
                AppLogger.LogError($"[BackupWindowViewModel] RestoreBackup failed for file '{SelectedBackup}'.");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            ErrorMessage = $"Eroare la restaurarea bazei de date: {ex.Message}";
            AppLogger.LogError($"[BackupWindowViewModel] Exception during restore: {ex.Message}", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void RefreshBackups()
    {
        try
        {
            AvailableBackups.Clear();
            var backups = _backupService.GetAvailableBackups();
            foreach (var backup in backups)
            {
                AvailableBackups.Add(backup);
            }

            SelectedBackup = AvailableBackups.FirstOrDefault();

            if (AvailableBackups.Count > 0)
            {
                AvailableBackupsCountMessage = $"Copii de siguranță disponibile: {AvailableBackups.Count}";
            }
            else
            {
                AvailableBackupsCountMessage = "Nicio copie de siguranță găsită în folderul Backups.";
            }

            var settings = _settingsService.GetSettings();
            if (settings.LastBackupDate.HasValue)
            {
                LastBackupDateFormatted = $"Ultimul backup: {settings.LastBackupDate.Value:dd.MM.yyyy HH:mm:ss}";
            }
            else
            {
                LastBackupDateFormatted = "Niciun backup efectuat încă.";
            }

            AppLogger.LogDebug($"[BackupWindowViewModel] Backups refreshed. Total available: {AvailableBackups.Count}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la reîmprospătarea backup-urilor: {ex.Message}";
            AppLogger.LogError($"[BackupWindowViewModel] Error refreshing backups: {ex.Message}", ex);
        }
    }

    [RelayCommand]
    private void ClearSelectedBackup()
    {
        SelectedBackup = null;
    }
}
