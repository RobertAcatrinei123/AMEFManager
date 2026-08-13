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

public partial class SealingDocumentGenerationWindowViewModel : ViewModelBase
{
    private readonly SealingDocumentService _sealingDocumentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;

    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private int _documentNumber;
    [ObservableProperty] private DateTimeOffset? _documentDate = DateTimeOffset.Now;

    public SealingDocumentGenerationWindowViewModel(
        AmefService amefService,
        SealingDocumentService sealingDocumentService,
        IDocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _sealingDocumentService = sealingDocumentService;
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;

        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;

        AmefMultiSelectUserControlViewModel.SelectedAmefs.CollectionChanged += async (s, e) =>
        {
            await OnSelectedAmefsChangedAsync();
        };

        _ = InitializeNextNumberAsync();
    }

    public SealingDocumentGenerationWindowViewModel(
        AmefMultiSelectUserControlViewModel amefMultiSelectUserControlViewModel,
        SealingDocumentService sealingDocumentService,
        IDocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _sealingDocumentService = sealingDocumentService;
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;

        AmefMultiSelectUserControlViewModel = amefMultiSelectUserControlViewModel;
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;

        AmefMultiSelectUserControlViewModel.SelectedAmefs.CollectionChanged += async (s, e) =>
        {
            await OnSelectedAmefsChangedAsync();
        };

        _ = InitializeNextNumberAsync();
    }

    private async Task InitializeNextNumberAsync()
    {
        try
        {
            DocumentNumber = await _sealingDocumentService.GetNextNumberAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"[SealingDocumentGenerationWindowViewModel] Could not initialize next number: {ex.Message}");
            if (DocumentNumber <= 0) DocumentNumber = 1;
        }
    }

    private async Task OnSelectedAmefsChangedAsync()
    {
        if (AmefMultiSelectUserControlViewModel.SelectedAmefs.Count == 1)
        {
            var amef = AmefMultiSelectUserControlViewModel.SelectedAmefs[0];
            try
            {
                var existing = await _sealingDocumentService.FindByAmefIdAsync(amef.Id);
                if (existing != null)
                {
                    DocumentNumber = existing.Number;
                    DocumentDate = new DateTimeOffset(existing.Date.ToDateTime(TimeOnly.MinValue));
                }
                else
                {
                    DocumentNumber = await _sealingDocumentService.GetNextNumberAsync();
                    DocumentDate = DateTimeOffset.Now;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"[SealingDocumentGenerationWindowViewModel] Error updating document number on selection change: {ex.Message}");
            }
        }
        else
        {
            await InitializeNextNumberAsync();
            DocumentDate = DateTimeOffset.Now;
        }
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
    private async Task GenerateSealingDocumentAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[SealingDocumentGenerationWindowViewModel.GenerateSealingDocumentAsync] Command execution started.");

        try
        {
            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[SealingDocumentGenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var selectedAmefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? [];
            if (selectedAmefs.Count == 0)
            {
                AppLogger.LogWarning("[SealingDocumentGenerationWindowViewModel] Validation failed: No AMEF selected.");
                ErrorMessage = "Trebuie să selectați un AMEF.";
                return;
            }
            if (selectedAmefs.Count > 1)
            {
                AppLogger.LogWarning("[SealingDocumentGenerationWindowViewModel] Validation failed: More than one AMEF selected.");
                ErrorMessage = "Trebuie să selectați exact un singur AMEF pentru generare.";
                return;
            }

            var amef = selectedAmefs[0];

            if (amef.Contract?.Client == null)
            {
                AppLogger.LogWarning($"[SealingDocumentGenerationWindowViewModel] Validation failed: AMEF {amef.Series} is not associated with a valid client.");
                ErrorMessage = "AMEF-ul selectat nu este asociat unui client valid.";
                return;
            }

            var date = DateOnly.FromDateTime(DateTime.Now);

            // Determine document number: reuse existing or compute next available
            var existing = await _sealingDocumentService.FindByAmefIdAsync(amef.Id);
            int documentNumber;
            if (existing != null)
            {
                documentNumber = existing.Number;
                existing.Date = date;
                await _sealingDocumentService.Update(existing);
                await _sealingDocumentService.SubmitChanges();
            }
            else
            {
                documentNumber = await _sealingDocumentService.GetNextNumberAsync();
                var newDoc = new SealingDocument
                {
                    Number = documentNumber,
                    Date = date,
                    AmefId = amef.Id,
                    Amef = amef
                };
                await _sealingDocumentService.Add(newDoc);
                await _sealingDocumentService.SubmitChanges();
            }

            DocumentNumber = documentNumber;
            DocumentDate = DateTimeOffset.Now;

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

            var targetDir = Path.Combine(outputBaseDir, clientFolder, sanitizedSeries);
            Directory.CreateDirectory(targetDir);

            string outputPath = Path.Combine(targetDir, "PV sigilare.docx");

            AppLogger.LogInfo($"[SealingDocumentGenerationWindowViewModel] Generating sealing document for AMEF '{amef.Series}' to '{outputPath}' (Nr={DocumentNumber})");
            await _documentGenerationService.GenerateSealingDocumentAsync(amef, outputPath, DocumentNumber, date);

            stopwatch.Stop();
            AppLogger.LogInfo($"[SealingDocumentGenerationWindowViewModel] Sealing document generated in {stopwatch.ElapsedMilliseconds} ms: '{outputPath}'");
            GenerationResult = "Generare cu succes!";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppLogger.LogError($"[SealingDocumentGenerationWindowViewModel] Sealing document generation failed after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}", ex);
            ErrorMessage = $"Eroare la generare: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
