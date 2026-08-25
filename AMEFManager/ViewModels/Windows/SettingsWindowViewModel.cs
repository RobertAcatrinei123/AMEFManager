using System;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.Windows;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private AppSettings _currentSettings;

    // --- Group 1: Date Societate (Service / Distribuitor) ---
    [ObservableProperty] private string _numeSocietate = string.Empty;
    [ObservableProperty] private string _cui = string.Empty;
    [ObservableProperty] private string _telefon = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _cnpTehnicianService = string.Empty;

    // --- Group 2: Contabilitate ---
    [ObservableProperty] private string _denumireContabil = string.Empty;
    [ObservableProperty] private string _cnpContabil = string.Empty;
    [ObservableProperty] private string _emailContabil = string.Empty;
    [ObservableProperty] private string _telContabil = string.Empty;

    // --- Group 3: Căi Sistem & Server ---
    [ObservableProperty] private string _serverPath = string.Empty;

    // --- Status & Feedback Properties ---
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSuccess;

    public SettingsWindowViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _currentSettings = _settingsService.GetSettings();
        LoadFromSettings(_currentSettings);
    }

    /// <summary>
    /// Populates ViewModel observable properties from an AppSettings model instance.
    /// </summary>
    public void LoadFromSettings(AppSettings settings)
    {
        _currentSettings = settings;

        // Group 1
        NumeSocietate = settings.NumeSocietate ?? string.Empty;
        Cui = settings.Cui ?? string.Empty;
        Telefon = settings.Telefon ?? string.Empty;
        Email = settings.Email ?? string.Empty;
        CnpTehnicianService = settings.CnpTehnicianService ?? string.Empty;

        // Group 2
        DenumireContabil = settings.DenumireContabil ?? string.Empty;
        CnpContabil = settings.CnpContabil ?? string.Empty;
        EmailContabil = settings.EmailContabil ?? string.Empty;
        TelContabil = settings.TelContabil ?? string.Empty;

        // Group 3
        ServerPath = settings.ServerPath ?? string.Empty;

        StatusMessage = null;
        ErrorMessage = null;
        IsSuccess = false;
    }

    /// <summary>
    /// Persists all observable properties to the underlying AppSettings model and file system.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        try
        {
            StatusMessage = null;
            ErrorMessage = null;
            IsSuccess = false;

            // Group 1
            _currentSettings.NumeSocietate = NumeSocietate?.Trim() ?? string.Empty;
            _currentSettings.Cui = Cui?.Trim() ?? string.Empty;
            _currentSettings.Telefon = Telefon?.Trim() ?? string.Empty;
            _currentSettings.Email = Email?.Trim() ?? string.Empty;
            _currentSettings.CnpTehnicianService = CnpTehnicianService?.Trim() ?? string.Empty;

            // Group 2
            _currentSettings.DenumireContabil = DenumireContabil?.Trim() ?? string.Empty;
            _currentSettings.CnpContabil = CnpContabil?.Trim() ?? string.Empty;
            _currentSettings.EmailContabil = EmailContabil?.Trim() ?? string.Empty;
            _currentSettings.TelContabil = TelContabil?.Trim() ?? string.Empty;

            // Group 3
            _currentSettings.ServerPath = ServerPath?.Trim() ?? string.Empty;

            _settingsService.SaveSettings(_currentSettings);

            StatusMessage = "Setările au fost salvate cu succes!";
            ErrorMessage = null;
            IsSuccess = true;
            AppLogger.LogInfo("[SettingsWindowViewModel] Application settings saved successfully.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la salvarea setărilor: {ex.Message}";
            StatusMessage = null;
            IsSuccess = false;
            AppLogger.LogError($"[SettingsWindowViewModel] SaveSettings failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reloads settings from disk, discarding any unsaved modifications.
    /// </summary>
    [RelayCommand]
    private void Reload()
    {
        try
        {
            _currentSettings = _settingsService.GetSettings();
            LoadFromSettings(_currentSettings);
            AppLogger.LogInfo("[SettingsWindowViewModel] Settings reloaded from disk.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la reîncărcarea setărilor: {ex.Message}";
            StatusMessage = null;
            IsSuccess = false;
            AppLogger.LogError($"[SettingsWindowViewModel] Reload settings failed: {ex.Message}", ex);
        }
    }
}
