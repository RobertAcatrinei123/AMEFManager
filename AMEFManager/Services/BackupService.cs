using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AMEFManager.Helpers;

namespace AMEFManager.Services;

public class BackupService
{
    private readonly SettingsService _settingsService;
    private readonly string _appDataFolder;
    private readonly string _backupsFolder;
    private readonly string _dbFilePath;

    public BackupService(SettingsService settingsService, string? appDataFolder = null)
    {
        _settingsService = settingsService;
        var folder = appDataFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMEFManager");
        _appDataFolder = folder;
        _backupsFolder = Path.Combine(_appDataFolder, "Backups");
        _dbFilePath = Path.Combine(_appDataFolder, "storage.sqlite");
        Directory.CreateDirectory(_backupsFolder);
        AppLogger.LogDebug($"[BackupService] Initialized with backups folder: '{_backupsFolder}'");
    }

    public List<string> GetAvailableBackups()
    {
        if (!Directory.Exists(_backupsFolder)) return new List<string>();
        var backups = Directory.GetFiles(_backupsFolder, "*.sqlite")
            .Select(Path.GetFileName)
            .Where(f => !string.IsNullOrEmpty(f))
            .Select(f => f!)
            .OrderByDescending(f => f)
            .ToList();
        AppLogger.LogDebug($"[BackupService] Found {backups.Count} available backup file(s).");
        return backups;
    }

    public bool PerformBackup()
    {
        var settings = _settingsService.GetSettings();
        if (settings.BackupIntervalDays <= 0 || settings.BackupsToKeep <= 0)
        {
            AppLogger.LogDebug("[BackupService] Backup skipped because BackupIntervalDays or BackupsToKeep is <= 0.");
            return false;
        }

        if (!File.Exists(_dbFilePath))
        {
            AppLogger.LogWarning($"[BackupService] Database file not found at '{_dbFilePath}', skipping backup.");
            return false;
        }

        try
        {
            string backupFileName = $"storage_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite";
            string backupFilePath = Path.Combine(_backupsFolder, backupFileName);
            AppLogger.LogInfo($"[BackupService] Creating database backup: '{backupFilePath}'");
            File.Copy(_dbFilePath, backupFilePath, true);
            settings.LastBackupDate = DateTime.Now;
            _settingsService.SaveSettings(settings);
            CleanupOldBackups(settings.BackupsToKeep);
            AppLogger.LogInfo($"[BackupService] Database backup completed successfully: '{backupFilePath}'");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"[BackupService] Error creating backup: {ex.Message}", ex);
            return false;
        }
    }

    private void CleanupOldBackups(int keepCount)
    {
        var backups = Directory.GetFiles(_backupsFolder, "*.sqlite")
            .OrderByDescending(f => f)
            .ToList();
        if (backups.Count > keepCount)
        {
            var backupsToDelete = backups.Skip(keepCount).ToList();
            AppLogger.LogInfo($"[BackupService] Cleaning up old backups. Retaining top {keepCount} files, deleting {backupsToDelete.Count} file(s).");
            foreach (var file in backupsToDelete)
            {
                try
                {
                    File.Delete(file);
                    AppLogger.LogDebug($"[BackupService] Deleted old backup file: '{file}'");
                }
                catch (Exception ex)
                {
                    AppLogger.LogWarning($"[BackupService] Failed to delete old backup file '{file}': {ex.Message}");
                }
            }
        }
    }

    public bool RestoreBackup(string backupFileName)
    {
        var backupFilePath = Path.Combine(_backupsFolder, backupFileName);
        if (!File.Exists(backupFilePath))
        {
            AppLogger.LogWarning($"[BackupService] Backup file not found at '{backupFilePath}', restore aborted.");
            return false;
        }

        try
        {
            AppLogger.LogInfo($"[BackupService] Restoring database from backup '{backupFilePath}' to '{_dbFilePath}'");
            File.Copy(backupFilePath, _dbFilePath, true);
            AppLogger.LogInfo($"[BackupService] Database restored successfully from '{backupFileName}'.");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"[BackupService] Error restoring backup '{backupFileName}': {ex.Message}", ex);
            return false;
        }
    }

    public void PerformStartupBackupCheck()
    {
        var settings = _settingsService.GetSettings();
        if (settings.BackupIntervalDays <= 0 || settings.BackupsToKeep <= 0) return;
        bool shouldBackup = false;
        if (!settings.LastBackupDate.HasValue)
        {
            shouldBackup = true;
        }
        else
        {
            var daysSinceLastBackup = (DateTime.Now - settings.LastBackupDate.Value).TotalDays;
            if (daysSinceLastBackup >= settings.BackupIntervalDays)
            {
                shouldBackup = true;
            }
        }
        if (shouldBackup)
        {
            AppLogger.LogInfo("[BackupService] Startup backup conditions met. Triggering backup...");
            PerformBackup();
        }
    }
}
