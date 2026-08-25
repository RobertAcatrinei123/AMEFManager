using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Services;
using AMEFManager.ViewModels.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AMEFManager.ViewModels.Windows;

public partial class InstallationDeclarationGenerationWindowViewModel : ViewModelBase, IDisposable
{
    private readonly AmefService _amefService;
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;
    private readonly ReasonService _reasonService;
    private bool _disposed;

    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isRefiscalizare;
    [ObservableProperty] private ObservableCollection<Reason> _reasons = [];
    [ObservableProperty] private Reason? _selectedReason;

    public InstallationDeclarationGenerationWindowViewModel(
        AmefService amefService,
        DocumentGenerationService documentGenerationService,
        SettingsService settingsService,
        ReasonService reasonService)
    {
        _amefService = amefService;
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
        _reasonService = reasonService;

        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;

        _ = LoadReasonsAsync();
    }

    [RelayCommand]
    public async Task LoadReasonsAsync()
    {
        try
        {
            var reasonsList = await _reasonService.FindAll();
            Reasons = new ObservableCollection<Reason>(reasonsList.OrderByDescending(r => r.Id));
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"[InstallationDeclarationGenerationWindowViewModel] Failed to load reasons: {ex.Message}");
        }
    }

    partial void OnIsRefiscalizareChanged(bool value)
    {
        if (!value)
        {
            SelectedReason = null;
        }
    }

    private List<string> ValidateAmefs(IEnumerable<Amef> amefs)
    {
        var errors = new List<string>();
        if (!amefs.Any())
        {
            errors.Add("Cel putin un AMEF trebuie selectat.");
        }
        return errors;
    }

    [RelayCommand]
    private async Task GenerateDeclarationsAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[InstallationDeclarationGenerationWindowViewModel.GenerateDeclarationsAsync] Command execution started.");

        try
        {
            var selectedAmefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? new List<Amef>();
            AppLogger.LogDebug($"[InstallationDeclarationGenerationWindowViewModel] Validating selection: {selectedAmefs.Count} AMEF(s) selected.");

            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[InstallationDeclarationGenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            if (IsRefiscalizare && SelectedReason == null)
            {
                ErrorMessage = "Va rugam sa selectati un motiv pentru refiscalizare.";
                AppLogger.LogWarning("[InstallationDeclarationGenerationWindowViewModel] Refiscalizare is enabled but no reason was selected.");
                return;
            }

            var settings = _settingsService.GetSettings();
            string outputBaseDir = !string.IsNullOrWhiteSpace(settings.ClientPath)
                ? settings.ClientPath
                : Path.Combine(settings.ServerPath, "Contracte Clientii");

            Directory.CreateDirectory(outputBaseDir);
            AppLogger.LogDebug($"[InstallationDeclarationGenerationWindowViewModel] Base output directory: '{outputBaseDir}'");

            int generatedCount = 0;
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var amef in selectedAmefs)
            {
                var client = amef.Contract?.Client;
                var contract = amef.Contract;
                string clientName = client?.Name ?? $"Client_{contract?.Number ?? 0}";
                string sanitizedClientName = string.Concat(clientName.Select(c => invalidChars.Contains(c) ? '_' : c)).Trim();
                if (string.IsNullOrWhiteSpace(sanitizedClientName))
                {
                    sanitizedClientName = $"Client_{contract?.Number ?? 0}";
                }

                string contractNum = contract != null ? contract.Number.ToString("D4") : "0000";
                string clientFolder = $"{contractNum} - {sanitizedClientName}";
                string sanitizedSeries = string.Concat((amef.Series ?? "Fara_Serie").Select(c => invalidChars.Contains(c) ? '_' : c)).Trim();
                if (string.IsNullOrWhiteSpace(sanitizedSeries))
                {
                    sanitizedSeries = "Fara_Serie";
                }

                string subFolder = IsRefiscalizare ? "refiscalizare" : "fiscalizare";
                var targetDir = Path.Combine(outputBaseDir, clientFolder, sanitizedSeries, subFolder);
                Directory.CreateDirectory(targetDir);

                string declarationPath = Path.Combine(targetDir, "DI.docx");

                await _documentGenerationService.GenerateInstallationDeclarationAsync(
                    amef,
                    declarationPath,
                    IsRefiscalizare,
                    SelectedReason?.Text);

                generatedCount++;
            }

            stopwatch.Stop();
            AppLogger.LogInfo($"[InstallationDeclarationGenerationWindowViewModel] Generated installation declarations for {generatedCount} AMEF(s) in {stopwatch.ElapsedMilliseconds} ms.");
            GenerationResult = "Generare cu succes!";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppLogger.LogError($"[InstallationDeclarationGenerationWindowViewModel] Installation declaration generation failed after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}", ex);
            ErrorMessage = $"Eroare la generare: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void ClearSelectedReason()
    {
        SelectedReason = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AmefMultiSelectUserControlViewModel.Dispose();
    }
}
