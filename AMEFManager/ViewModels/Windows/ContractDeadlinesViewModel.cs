using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.Windows;

public partial class ContractDeadlinesViewModel : ViewModelBase, IDisposable
{
    private readonly ContractService _contractService;
    private readonly Func<DateOnly>? _todayProvider;
    private DateOnly? _overrideReferenceDate;
    private List<Contract> _allContracts = [];

    [ObservableProperty]
    private int _thresholdDays = 5;

    [ObservableProperty]
    private ObservableCollection<Contract> _filteredContracts = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    public int DaysThreshold
    {
        get => ThresholdDays;
        set => ThresholdDays = value;
    }

    public ObservableCollection<Contract> ImpendingContracts => FilteredContracts;

    public DateOnly Today => _overrideReferenceDate ?? _todayProvider?.Invoke() ?? DateOnly.FromDateTime(DateTime.Today);

    public ContractDeadlinesViewModel(ContractService contractService, Func<DateOnly>? todayProvider = null)
    {
        _contractService = contractService ?? throw new ArgumentNullException(nameof(contractService));
        _todayProvider = todayProvider;
        _ = LoadContractsAsync();
    }

    partial void OnThresholdDaysChanged(int value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    public async Task LoadContractsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            _allContracts = await _contractService.FindAll();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to load contracts in ContractDeadlinesViewModel: {ex.Message}", ex);
            ErrorMessage = $"Eroare la încărcarea contractelor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ApplyFilter(DateOnly? referenceDate = null)
    {
        if (referenceDate.HasValue)
        {
            _overrideReferenceDate = referenceDate;
        }

        var baseDate = _overrideReferenceDate ?? _todayProvider?.Invoke() ?? DateOnly.FromDateTime(DateTime.Today);
        var maxDate = baseDate.AddDays(ThresholdDays);

        var filtered = _allContracts
            .Where(c => c.IsActive && c.ValidUntil <= maxDate)
            .OrderBy(c => c.ValidUntil)
            .ThenBy(c => c.Number)
            .ToList();

        FilteredContracts = new ObservableCollection<Contract>(filtered);
        OnPropertyChanged(nameof(ImpendingContracts));
    }

    [RelayCommand]
    public async Task DeactivateContractAsync(Contract contract)
    {
        if (contract == null) return;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            contract.IsActive = false;
            var existing = _allContracts.FirstOrDefault(c => c.Id == contract.Id);
            if (existing != null && !ReferenceEquals(existing, contract))
            {
                existing.IsActive = false;
            }

            await _contractService.Update(contract);
            await _contractService.SubmitChanges();
            ApplyFilter();
            StatusMessage = $"Contractul #{contract.Number} a fost dezactivat cu succes.";
            AppLogger.LogInfo($"Contract #{contract.Number} deactivated via ContractDeadlines.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to deactivate contract #{contract.Number}: {ex.Message}", ex);
            ErrorMessage = $"Eroare la dezactivarea contractului: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ExtendOneYearAsync(Contract contract)
    {
        if (contract == null) return;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            contract.ValidUntil = contract.ValidUntil.AddYears(1);
            var existing = _allContracts.FirstOrDefault(c => c.Id == contract.Id);
            if (existing != null && !ReferenceEquals(existing, contract))
            {
                existing.ValidUntil = contract.ValidUntil;
            }

            await _contractService.Update(contract);
            await _contractService.SubmitChanges();
            ApplyFilter();
            StatusMessage = $"Contractul #{contract.Number} a fost prelungit cu 1 an (valabil până la {contract.ValidUntil:dd.MM.yyyy}).";
            AppLogger.LogInfo($"Contract #{contract.Number} extended by 1 year to {contract.ValidUntil}.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to extend contract #{contract.Number}: {ex.Message}", ex);
            ErrorMessage = $"Eroare la prelungirea contractului: {ex.Message}";
        }
    }

    public Task ExtendContractOneYearAsync(Contract contract) => ExtendOneYearAsync(contract);

    public void Dispose()
    {
        FilteredContracts.Clear();
        _allContracts.Clear();
    }
}
