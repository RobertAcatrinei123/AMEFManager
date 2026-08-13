using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AMEFManager.Helpers;
using AMEFManager.Models;

namespace AMEFManager.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;

    public static Func<string>? DefaultPathResolver { get; set; }

    public SettingsService(string? customSettingsFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customSettingsFilePath))
        {
            _settingsFilePath = customSettingsFilePath;
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        else if (DefaultPathResolver != null)
        {
            _settingsFilePath = DefaultPathResolver();
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        else if (IsTestExecutionEnvironment())
        {
            var testDir = Path.Combine(Path.GetTempPath(), "AMEFManager_Test_Settings");
            Directory.CreateDirectory(testDir);
            _settingsFilePath = Path.Combine(testDir, "appsettings.json");
        }
        else
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataFolder, "AMEFManager");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "appsettings.json");
        }

        AppLogger.LogDebug($"[SettingsService] Initialized with config path: '{_settingsFilePath}'");
        _currentSettings = LoadSettings();
    }

    private static bool IsTestExecutionEnvironment()
    {
        var entry = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
        if (entry != null && (entry.Contains("testhost", StringComparison.OrdinalIgnoreCase) || 
                              entry.Contains("xunit", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return AppDomain.CurrentDomain.GetAssemblies().Any(a =>
        {
            var name = a.GetName().Name;
            return name != null && (name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                                   name.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) ||
                                   name.StartsWith("Microsoft.TestPlatform", StringComparison.OrdinalIgnoreCase));
        });
    }

    public string SettingsFilePath => _settingsFilePath;

    public AppSettings GetSettings()
    {
        return _currentSettings;
    }

    public void SaveSettings(AppSettings settings)
    {
        _currentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        try
        {
            AppLogger.LogInfo($"[SettingsService] Saving settings to '{_settingsFilePath}'");
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
            AppLogger.LogDebug("[SettingsService] Settings saved successfully.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"[SettingsService] Failed to save settings to '{_settingsFilePath}': {ex.Message}", ex);
            throw;
        }
    }

    private AppSettings LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                AppLogger.LogDebug($"[SettingsService] Loading settings from '{_settingsFilePath}'");
                var json = File.ReadAllText(_settingsFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                if (settings != null)
                {
                    AppLogger.LogDebug("[SettingsService] Settings loaded successfully.");
                    return settings;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"[SettingsService] Failed to parse settings file '{_settingsFilePath}': {ex.Message}. Falling back to default settings.");
            }
        }
        else
        {
            AppLogger.LogDebug($"[SettingsService] Settings file '{_settingsFilePath}' does not exist, creating default settings.");
        }
        return new AppSettings();
    }
}
