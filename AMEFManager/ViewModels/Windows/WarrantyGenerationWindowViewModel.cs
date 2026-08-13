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

public partial class WarrantyGenerationWindowViewModel : ViewModelBase
{
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;

    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    public WarrantyGenerationWindowViewModel(
        AmefService amefService,
        IDocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;
    }

    public WarrantyGenerationWindowViewModel(
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
    private async Task GenerateWarrantyAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[WarrantyGenerationWindowViewModel.GenerateWarrantyAsync] Command execution started.");

        try
        {
            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[WarrantyGenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var selectedAmefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? [];
            if (selectedAmefs.Count == 0)
            {
                AppLogger.LogWarning("[WarrantyGenerationWindowViewModel] Validation failed: No AMEF selected.");
                ErrorMessage = "Trebuie să selectați un AMEF.";
                return;
            }
            if (selectedAmefs.Count > 1)
            {
                AppLogger.LogWarning("[WarrantyGenerationWindowViewModel] Validation failed: More than one AMEF selected.");
                ErrorMessage = "Trebuie să selectați exact un singur AMEF pentru generare.";
                return;
            }

            var amef = selectedAmefs[0];

            if (amef.Bill == null)
            {
                AppLogger.LogWarning($"[WarrantyGenerationWindowViewModel] Validation failed: AMEF {amef.Series} has no associated Bill.");
                ErrorMessage = "AMEF-ul selectat nu are o factură de achiziție asociată.";
                return;
            }

            if (string.IsNullOrWhiteSpace(amef.GetBrand()))
            {
                AppLogger.LogWarning($"[WarrantyGenerationWindowViewModel] Validation failed: AMEF {amef.Series} has no Brand specified in authorization.");
                ErrorMessage = "AMEF-ul selectat nu are un brand specificat în autorizație.";
                return;
            }

            if (amef.Contract?.Client == null)
            {
                AppLogger.LogWarning($"[WarrantyGenerationWindowViewModel] Validation failed: AMEF {amef.Series} is not associated with a valid client.");
                ErrorMessage = "AMEF-ul selectat nu este asociat unui client valid.";
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
            AppLogger.LogDebug($"[WarrantyGenerationWindowViewModel] Target client output directory: '{targetDir}'");

            string outputPath = Path.Combine(targetDir, "Certificat de garantie.pdf");

            AppLogger.LogInfo($"[WarrantyGenerationWindowViewModel] Generating warranty for AMEF '{amef.Series}' to '{outputPath}'");
            await _documentGenerationService.GenerateWarrantyAsync(amef, outputPath);

            stopwatch.Stop();
            AppLogger.LogInfo($"[WarrantyGenerationWindowViewModel] Warranty document generated in {stopwatch.ElapsedMilliseconds} ms: '{outputPath}'");
            GenerationResult = "Generare cu succes!";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppLogger.LogError($"[WarrantyGenerationWindowViewModel] Warranty generation failed after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}", ex);
            ErrorMessage = $"Eroare la generare: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
