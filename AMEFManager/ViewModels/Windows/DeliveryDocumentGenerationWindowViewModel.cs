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

using System.Collections.Specialized;

namespace AMEFManager.ViewModels.Windows;

public partial class DeliveryDocumentGenerationWindowViewModel : ViewModelBase, IDisposable
{
    private readonly DeliveryDocumentService _deliveryDocumentService;
    private readonly ReasonService _reasonService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;
    private readonly NotifyCollectionChangedEventHandler _selectedAmefsHandler;
    private bool _disposed;

    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private int _documentNumber;
    [ObservableProperty] private DateTimeOffset? _documentDate = DateTimeOffset.Now;

    [ObservableProperty] private ObservableCollection<Reason> _reasons = [];
    [ObservableProperty] private Reason? _selectedReason;

    public DeliveryDocumentGenerationWindowViewModel(
        AmefService amefService,
        DeliveryDocumentService deliveryDocumentService,
        ReasonService reasonService,
        IDocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _deliveryDocumentService = deliveryDocumentService;
        _reasonService = reasonService;
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;

        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;

        _selectedAmefsHandler = async (s, e) =>
        {
            await OnSelectedAmefsChangedAsync();
        };
        AmefMultiSelectUserControlViewModel.SelectedAmefs.CollectionChanged += _selectedAmefsHandler;

        _ = InitializeNextNumberAsync();
        _ = LoadReasonsAsync();
    }

    public DeliveryDocumentGenerationWindowViewModel(
        AmefMultiSelectUserControlViewModel amefMultiSelectUserControlViewModel,
        DeliveryDocumentService deliveryDocumentService,
        ReasonService reasonService,
        IDocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _deliveryDocumentService = deliveryDocumentService;
        _reasonService = reasonService;
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;

        AmefMultiSelectUserControlViewModel = amefMultiSelectUserControlViewModel;
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;

        _selectedAmefsHandler = async (s, e) =>
        {
            await OnSelectedAmefsChangedAsync();
        };
        AmefMultiSelectUserControlViewModel.SelectedAmefs.CollectionChanged += _selectedAmefsHandler;

        _ = InitializeNextNumberAsync();
        _ = LoadReasonsAsync();
    }

    [RelayCommand]
    public async Task LoadReasonsAsync()
    {
        try
        {
            var reasonsList = await _reasonService.FindAll();
            Reasons = new ObservableCollection<Reason>(reasonsList.OrderBy(r => r.Text));
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"[DeliveryDocumentGenerationWindowViewModel] Could not load reasons: {ex.Message}");
        }
    }

    private async Task InitializeNextNumberAsync()
    {
        try
        {
            DocumentNumber = await _deliveryDocumentService.GetNextNumberAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"[DeliveryDocumentGenerationWindowViewModel] Could not initialize next number: {ex.Message}");
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
                var existing = await _deliveryDocumentService.FindByAmefIdAsync(amef.Id);
                if (existing != null)
                {
                    DocumentNumber = existing.Number;
                    DocumentDate = DateHelper.ToDateTimeOffset(existing.Date);
                }
                else
                {
                    DocumentNumber = await _deliveryDocumentService.GetNextNumberAsync();
                    DocumentDate = DateTimeOffset.Now;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"[DeliveryDocumentGenerationWindowViewModel] Error updating document number on selection change: {ex.Message}");
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
    private async Task GenerateDeliveryDocumentAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[DeliveryDocumentGenerationWindowViewModel.GenerateDeliveryDocumentAsync] Command execution started.");

        try
        {
            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[DeliveryDocumentGenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var selectedAmefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? [];
            if (selectedAmefs.Count == 0)
            {
                AppLogger.LogWarning("[DeliveryDocumentGenerationWindowViewModel] Validation failed: No AMEF selected.");
                ErrorMessage = "Trebuie să selectați un AMEF.";
                return;
            }
            if (selectedAmefs.Count > 1)
            {
                AppLogger.LogWarning("[DeliveryDocumentGenerationWindowViewModel] Validation failed: More than one AMEF selected.");
                ErrorMessage = "Trebuie să selectați exact un singur AMEF pentru generare.";
                return;
            }

            if (SelectedReason == null || string.IsNullOrWhiteSpace(SelectedReason.Text))
            {
                ErrorMessage = "Vă rugăm să selectați un motiv pentru predare.";
                return;
            }

            var amef = selectedAmefs[0];

            if (amef.Contract?.Client == null)
            {
                AppLogger.LogWarning($"[DeliveryDocumentGenerationWindowViewModel] Validation failed: AMEF {amef.Series} is not associated with a valid client.");
                ErrorMessage = "AMEF-ul selectat nu este asociat unui client valid.";
                return;
            }

            var date = DateOnly.FromDateTime(DateTime.Now);

            // Determine document number: reuse existing or compute next available
            var existing = await _deliveryDocumentService.FindByAmefIdAsync(amef.Id);
            int documentNumber;
            if (existing != null)
            {
                documentNumber = existing.Number;
                existing.Date = date;
                await _deliveryDocumentService.Update(existing);
                await _deliveryDocumentService.SubmitChanges();
            }
            else
            {
                documentNumber = await _deliveryDocumentService.GetNextNumberAsync();
                var newDoc = new DeliveryDocument
                {
                    Number = documentNumber,
                    Date = date,
                    AmefId = amef.Id,
                    Amef = amef
                };
                await _deliveryDocumentService.Add(newDoc);
                await _deliveryDocumentService.SubmitChanges();
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

            var targetDir = Path.Combine(outputBaseDir, clientFolder, sanitizedSeries, "refiscalizare");
            Directory.CreateDirectory(targetDir);

            string outputPath = Path.Combine(targetDir, "PV predare.docx");

            AppLogger.LogInfo($"[DeliveryDocumentGenerationWindowViewModel] Generating delivery document for AMEF '{amef.Series}' to '{outputPath}' (Nr={documentNumber}, Reason='{SelectedReason.Text}')");
            await _documentGenerationService.GenerateDeliveryDocumentAsync(amef, outputPath, documentNumber, SelectedReason.Text, date);

            stopwatch.Stop();
            AppLogger.LogInfo($"[DeliveryDocumentGenerationWindowViewModel] Delivery document generated in {stopwatch.ElapsedMilliseconds} ms: '{outputPath}'");
            GenerationResult = "Generare cu succes!";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppLogger.LogError($"[DeliveryDocumentGenerationWindowViewModel] Delivery document generation failed after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}", ex);
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

        AmefMultiSelectUserControlViewModel.SelectedAmefs.CollectionChanged -= _selectedAmefsHandler;
        AmefMultiSelectUserControlViewModel.Dispose();
    }
}
