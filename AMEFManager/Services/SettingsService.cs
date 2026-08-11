using System;
using System.IO;
using System.Text.Json;
using AMEFManager.Models;
namespace AMEFManager.Services;
public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;
    public SettingsService()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataFolder, "AMEFManager");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "appsettings.json");
        _currentSettings = LoadSettings();
    }
    public AppSettings GetSettings()
    {
        return _currentSettings;
    }
    public void SaveSettings(AppSettings settings)
    {
        _currentSettings = settings;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }
    private AppSettings LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
            catch
            {
            }
        }
        return new AppSettings();
    }
}
