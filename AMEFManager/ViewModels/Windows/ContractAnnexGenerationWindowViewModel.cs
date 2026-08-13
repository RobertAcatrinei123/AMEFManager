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

public partial class ContractAnnexGenerationWindowViewModel : ViewModelBase
{
    private readonly ContractService _contractService;
    private readonly AdditionalDocumentService _additionalDocumentService;
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;

    public ContractMultiSelectUserControlViewModel ContractMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    public ContractAnnexGenerationWindowViewModel(
        ContractService contractService,
        AdditionalDocumentService additionalDocumentService,
        DocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _contractService = contractService;
        _additionalDocumentService = additionalDocumentService;
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;

        ContractMultiSelectUserControlViewModel = new ContractMultiSelectUserControlViewModel(contractService);
        ContractMultiSelectUserControlViewModel.Validator = ValidateContracts;
    }

    private List<string> ValidateContracts(IEnumerable<Contract> contracts)
    {
        var errors = new List<string>();
        if (!contracts.Any())
        {
            errors.Add("Cel putin un contract trebuie selectat.");
        }
        return errors;
    }

    [RelayCommand]
    private async Task GenerateAnnexesAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[ContractAnnexGenerationWindowViewModel.GenerateAnnexesAsync] Command execution started.");

        try
        {
            var selectedContracts = ContractMultiSelectUserControlViewModel.SelectedContracts?.ToList() ?? new List<Contract>();
            AppLogger.LogDebug($"[ContractAnnexGenerationWindowViewModel] Validating selection: {selectedContracts.Count} contract(s) selected.");

            var validationErrors = ContractMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[ContractAnnexGenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var settings = _settingsService.GetSettings();
            string outputBaseDir = !string.IsNullOrWhiteSpace(settings.ClientPath)
                ? settings.ClientPath
                : Path.Combine(settings.ServerPath, "Contracte Clientii");

            Directory.CreateDirectory(outputBaseDir);
            AppLogger.LogDebug($"[ContractAnnexGenerationWindowViewModel] Base output directory: '{outputBaseDir}'");

            int generatedCount = 0;
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var contract in selectedContracts)
            {
                string clientName = contract.Client?.Name ?? $"Client_{contract.Number}";
                string sanitizedClientName = string.Concat(clientName.Select(c => invalidChars.Contains(c) ? '_' : c)).Trim();
                if (string.IsNullOrWhiteSpace(sanitizedClientName))
                {
                    sanitizedClientName = $"Client_{contract.Number}";
                }

                string folderName = $"{contract.Number:D4} - {sanitizedClientName}";
                var clientDir = Path.Combine(outputBaseDir, folderName);
                Directory.CreateDirectory(clientDir);

                int nextNr = await _additionalDocumentService.GetNextNumberForContractAsync(contract.Id);
                var newDoc = new AdditionalDocument
                {
                    Number = nextNr,
                    Date = DateOnly.FromDateTime(DateTime.Now),
                    ContractId = contract.Id,
                    Contract = contract
                };

                await _additionalDocumentService.Add(newDoc);
                await _additionalDocumentService.SubmitChanges();

                string annexPath = Path.Combine(clientDir, $"AA {nextNr}.docx");
                await _documentGenerationService.GenerateContractAnnexAsync(newDoc, annexPath);

                generatedCount++;
            }

            stopwatch.Stop();
            AppLogger.LogInfo($"[ContractAnnexGenerationWindowViewModel] Generated annexes for {generatedCount} contract(s) in {stopwatch.ElapsedMilliseconds} ms.");
            GenerationResult = "Generare cu succes!";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppLogger.LogError($"[ContractAnnexGenerationWindowViewModel] Contract annex generation failed after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}", ex);
            ErrorMessage = $"Eroare la generare: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
