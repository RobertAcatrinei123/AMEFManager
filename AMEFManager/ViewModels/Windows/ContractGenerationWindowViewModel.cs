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

public partial class ContractGenerationWindowViewModel : ViewModelBase
{
    private readonly ContractService _contractService;
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;

    public ContractMultiSelectUserControlViewModel ContractMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    public ContractGenerationWindowViewModel(
        ContractService contractService,
        DocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _contractService = contractService;
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
    private async Task GenerateContractsAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[ContractGenerationWindowViewModel.GenerateContractsAsync] Command execution started.");

        try
        {
            var selectedContracts = ContractMultiSelectUserControlViewModel.SelectedContracts?.ToList() ?? new List<Contract>();
            AppLogger.LogDebug($"[ContractGenerationWindowViewModel] Validating selection: {selectedContracts.Count} contract(s) selected.");

            var validationErrors = ContractMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[ContractGenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var settings = _settingsService.GetSettings();
            string outputBaseDir = !string.IsNullOrWhiteSpace(settings.ClientPath)
                ? settings.ClientPath
                : Path.Combine(settings.ServerPath, "Contracte Clientii");

            Directory.CreateDirectory(outputBaseDir);
            AppLogger.LogDebug($"[ContractGenerationWindowViewModel] Base output directory: '{outputBaseDir}'");

            int generatedCount = 0;
            foreach (var contract in selectedContracts)
            {
                string clientName = contract.Client?.Name ?? $"Client_{contract.Number}";
                var invalidChars = Path.GetInvalidFileNameChars();
                string sanitizedClientName = string.Concat(clientName.Select(c => invalidChars.Contains(c) ? '_' : c)).Trim();
                if (string.IsNullOrWhiteSpace(sanitizedClientName))
                {
                    sanitizedClientName = $"Client_{contract.Number}";
                }

                string folderName = $"{contract.Number:D4} - {sanitizedClientName}";
                var clientDir = Path.Combine(outputBaseDir, folderName);
                Directory.CreateDirectory(clientDir);

                string serviceContractPath = Path.Combine(clientDir, $"{contract.Number:D4} - {sanitizedClientName}.docx");
                string paperContractPath = Path.Combine(clientDir, $"{contract.Number:D4}R - {sanitizedClientName}.docx");

                await _documentGenerationService.GenerateServiceContractAsync(contract, serviceContractPath);
                await _documentGenerationService.GeneratePaperContractAsync(contract, paperContractPath);

                generatedCount++;
            }

            stopwatch.Stop();
            AppLogger.LogInfo($"[ContractGenerationWindowViewModel] Generated contracts for {generatedCount} client(s) in {stopwatch.ElapsedMilliseconds} ms.");
            GenerationResult = "Generare cu succes!";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppLogger.LogError($"[ContractGenerationWindowViewModel] Contract generation failed after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}", ex);
            ErrorMessage = $"Eroare la generare: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
