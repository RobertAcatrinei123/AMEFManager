using System;

namespace AMEFManager.Models;
public class AppSettings
{
    // Existing Path & Backup Properties
    public int BackupIntervalDays { get; set; } = 0;
    public int BackupsToKeep { get; set; } = 0;
    public DateTime? LastBackupDate { get; set; }
    
    public string ServerPath { get; set; } = string.Empty;

    public string ANAFDocumentsPath => System.IO.Path.Combine(ServerPath, $"{DateTime.Now.Year} DECLARATII");

    public string ClientPath => System.IO.Path.Combine(ServerPath, "Contracte Clientii");

    // Group 1: Date Societate (Service / Distribuitor)
    public string NumeSocietate { get; set; } = string.Empty;
    public string Cui { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Group 2: Contabilitate
    public string DenumireContabil { get; set; } = string.Empty;
    public string CnpContabil { get; set; } = string.Empty;
    public string EmailContabil { get; set; } = string.Empty;
    public string TelContabil { get; set; } = string.Empty;
}
