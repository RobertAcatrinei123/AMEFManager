using System;
using System.Collections.Generic;
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

namespace AMEFManager.ViewModels.Windows;

public partial class AuthorizationGenerationWindowViewModel : ViewModelBase
{
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;

    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    public AuthorizationGenerationWindowViewModel(
        AmefService amefService,
        IDocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;
    }

    public AuthorizationGenerationWindowViewModel(
        AmefMultiSelectUserControlViewModel amefMultiSelectUserControlViewModel,
        IDocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        AmefMultiSelectUserControlViewModel = amefMultiSelectUserControlViewModel;
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
    }

    private List<string> ValidateAmefs(IEnumerable<Amef> amefs)
    {
        var errors = new List<string>();
        var list = amefs?.ToList() ?? [];
        if (list.Count == 0)
        {
            errors.Add("Trebuie să selectați un AMEF.");
        }
        else if (list.Count > 1)
        {
            errors.Add("Trebuie să selectați exact un singur AMEF pentru generare.");
        }
        return errors;
    }

    [RelayCommand]
    private async Task GenerateAuthorizationAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[AuthorizationGenerationWindowViewModel.GenerateAuthorizationAsync] Command execution started.");

        try
        {
            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[AuthorizationGenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var selectedAmefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? [];
            if (selectedAmefs.Count == 0)
            {
                AppLogger.LogWarning("[AuthorizationGenerationWindowViewModel] Validation failed: No AMEF selected.");
                ErrorMessage = "Trebuie să selectați un AMEF.";
                return;
            }
            if (selectedAmefs.Count > 1)
            {
                AppLogger.LogWarning("[AuthorizationGenerationWindowViewModel] Validation failed: More than one AMEF selected.");
                ErrorMessage = "Trebuie să selectați exact un singur AMEF pentru generare.";
                return;
            }

            var amef = selectedAmefs[0];

            if (amef.Authorization == null)
            {
                AppLogger.LogWarning($"[AuthorizationGenerationWindowViewModel] Validation failed: AMEF {amef.Series} has no associated Authorization.");
                ErrorMessage = "AMEF-ul selectat nu are o autorizație asociată.";
                return;
            }

            if (amef.Contract?.Client == null)
            {
                AppLogger.LogWarning($"[AuthorizationGenerationWindowViewModel] Validation failed: AMEF {amef.Series} is not associated with a valid client (prin contract).");
                ErrorMessage = "AMEF-ul selectat nu este asociat unui client valid (prin contract).";
                return;
            }

            var settings = _settingsService.GetSettings();
            string outputBaseDir = !string.IsNullOrWhiteSpace(settings.ClientPath)
                ? settings.ClientPath
                : Path.Combine(settings.ServerPath, "Contracte Clientii");

            var client = amef.Contract.Client;
            var contract = amef.Contract;
            var invalidChars = Path.GetInvalidFileNameChars();
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

            var targetDir = Path.Combine(outputBaseDir, clientFolder, sanitizedSeries, "Atribuire NUI", "Acte Atribuire NUI");
            Directory.CreateDirectory(targetDir);
            AppLogger.LogDebug($"[AuthorizationGenerationWindowViewModel] Target client output directory: '{targetDir}'");

            string outputPath = Path.Combine(targetDir, $"AA {amef.Authorization.Number}.pdf");

            AppLogger.LogInfo($"[AuthorizationGenerationWindowViewModel] Copying authorization for AMEF '{amef.Series}' to '{outputPath}'");
            await _documentGenerationService.CopyAuthorizationAsync(amef, outputPath);

            stopwatch.Stop();
            AppLogger.LogInfo($"[AuthorizationGenerationWindowViewModel] Authorization document generated in {stopwatch.ElapsedMilliseconds} ms: '{outputPath}'");
            GenerationResult = "Generare cu succes!";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppLogger.LogError($"[AuthorizationGenerationWindowViewModel] Authorization generation failed after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}", ex);
            ErrorMessage = $"Eroare la generare: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
