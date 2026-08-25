using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AMEFManager.Helpers;
using Microsoft.Data.Sqlite;

namespace AMEFManager.Services;

public class BackupService
{
    private static readonly SemaphoreSlim _syncLock = new(1, 1);
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
        _syncLock.Wait();
        try
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

                bool backupSuccess = false;
                try
                {
                    using (var sourceConn = new SqliteConnection($"Data Source={_dbFilePath};Mode=ReadOnly"))
                    using (var destConn = new SqliteConnection($"Data Source={backupFilePath}"))
                    {
                        sourceConn.Open();
                        destConn.Open();
                        sourceConn.BackupDatabase(destConn);
                    }
                    backupSuccess = true;
                }
                catch (Exception ex)
                {
                    AppLogger.LogWarning($"[BackupService] SqliteConnection.BackupDatabase failed ({ex.Message}). Falling back to File.Copy.");
                    if (File.Exists(backupFilePath))
                    {
                        try { File.Delete(backupFilePath); } catch { }
                    }
                    File.Copy(_dbFilePath, backupFilePath, true);
                    backupSuccess = true;
                }

                if (backupSuccess)
                {
                    settings.LastBackupDate = DateTime.Now;
                    _settingsService.SaveSettings(settings);
                    CleanupOldBackups(settings.BackupsToKeep);
                    AppLogger.LogInfo($"[BackupService] Database backup completed successfully: '{backupFilePath}'");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[BackupService] Error creating backup: {ex.Message}", ex);
                return false;
            }
        }
        finally
        {
            _syncLock.Release();
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
        if (string.IsNullOrWhiteSpace(backupFileName))
        {
            AppLogger.LogWarning("[BackupService] Empty or invalid backup filename provided, restore aborted.");
            return false;
        }

        var backupFilePath = Path.Combine(_backupsFolder, backupFileName);
        if (!File.Exists(backupFilePath))
        {
            AppLogger.LogWarning($"[BackupService] Backup file not found at '{backupFilePath}', restore aborted.");
            return false;
        }

        _syncLock.Wait();
        string tempRestorePath = _dbFilePath + ".restore_tmp";
        string safetyBakPath = _dbFilePath + ".restore_bak";
        string walPath = _dbFilePath + "-wal";
        string shmPath = _dbFilePath + "-shm";
        string journalPath = _dbFilePath + "-journal";

        try
        {
            AppLogger.LogInfo($"[BackupService] Restoring database from backup '{backupFilePath}' to '{_dbFilePath}'");

            // 1. Stage the backup file to a temporary file first
            File.Copy(backupFilePath, tempRestorePath, true);

            // 2. Clear all active SQLite connection pools to release locks
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // 3. Create safety backup of existing database if present
            if (File.Exists(_dbFilePath))
            {
                File.Copy(_dbFilePath, safetyBakPath, true);
            }

            // 4. Delete existing active DB and sidecar files to prevent stale WAL contamination
            if (File.Exists(_dbFilePath)) File.Delete(_dbFilePath);
            if (File.Exists(walPath)) File.Delete(walPath);
            if (File.Exists(shmPath)) File.Delete(shmPath);
            if (File.Exists(journalPath)) File.Delete(journalPath);

            // 5. Move staged file to active database path
            File.Move(tempRestorePath, _dbFilePath);

            // 6. Delete safety backup upon successful restore
            if (File.Exists(safetyBakPath))
            {
                try { File.Delete(safetyBakPath); } catch { }
            }

            SqliteConnection.ClearAllPools();
            AppLogger.LogInfo($"[BackupService] Database restored successfully from '{backupFileName}'.");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"[BackupService] Error restoring backup '{backupFileName}': {ex.Message}", ex);

            // Rollback from safety backup if available and main DB was deleted
            if (File.Exists(safetyBakPath) && !File.Exists(_dbFilePath))
            {
                try
                {
                    File.Move(safetyBakPath, _dbFilePath);
                    AppLogger.LogInfo("[BackupService] Successfully rolled back to pre-restore safety backup.");
                }
                catch (Exception rollbackEx)
                {
                    AppLogger.LogError($"[BackupService] Failed to rollback from safety backup: {rollbackEx.Message}", rollbackEx);
                }
            }

            return false;
        }
        finally
        {
            if (File.Exists(tempRestorePath))
            {
                try { File.Delete(tempRestorePath); } catch { }
            }
            if (File.Exists(safetyBakPath))
            {
                try { File.Delete(safetyBakPath); } catch { }
            }
            SqliteConnection.ClearAllPools();
            _syncLock.Release();
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
