using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace AMEFManager.Services;
public class BackupService
{
    private readonly SettingsService _settingsService;
    private readonly string _appDataFolder;
    private readonly string _backupsFolder;
    private readonly string _dbFilePath;
    public BackupService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _appDataFolder = Path.Combine(folder, "AMEFManager");
        _backupsFolder = Path.Combine(_appDataFolder, "Backups");
        _dbFilePath = Path.Combine(_appDataFolder, "storage.sqlite");
        Directory.CreateDirectory(_backupsFolder);
    }
    public List<string> GetAvailableBackups()
    {
        if (!Directory.Exists(_backupsFolder)) return new List<string>();
        return Directory.GetFiles(_backupsFolder, "*.sqlite")
            .Select(Path.GetFileName)
            .OrderByDescending(f => f)
            .ToList()!;
    }
    public bool PerformBackup()
    {
        var settings = _settingsService.GetSettings();
        if (settings.BackupIntervalDays <= 0 || settings.BackupsToKeep <= 0) return false;
        if (!File.Exists(_dbFilePath)) return false;
        try
        {
            string backupFileName = $"storage_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite";
            string backupFilePath = Path.Combine(_backupsFolder, backupFileName);
            File.Copy(_dbFilePath, backupFilePath, true);
            settings.LastBackupDate = DateTime.Now;
            _settingsService.SaveSettings(settings);
            CleanupOldBackups(settings.BackupsToKeep);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Eroare la creare backup: {ex.Message}");
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
            var backupsToDelete = backups.Skip(keepCount);
            foreach (var file in backupsToDelete)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                }
            }
        }
    }
    public bool RestoreBackup(string backupFileName)
    {
        var backupFilePath = Path.Combine(_backupsFolder, backupFileName);
        if (!File.Exists(backupFilePath)) return false;
        try
        {
            File.Copy(backupFilePath, _dbFilePath, true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Eroare la restaurare backup: {ex.Message}");
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
            PerformBackup();
        }
    }
}
