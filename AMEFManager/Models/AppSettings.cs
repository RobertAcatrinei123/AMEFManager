using System;

namespace AMEFManager.Models;
public class AppSettings
{
    public int BackupIntervalDays { get; set; } = 0;
    public int BackupsToKeep { get; set; } = 0;
    public DateTime? LastBackupDate { get; set; }
    
    public string ServerPath { get; set; } = string.Empty;
}
