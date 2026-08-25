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

namespace AMEFManager.ViewModels.UserControls;

public partial class ContractTypeUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly ContractTypeService _contractTypeService;
    private List<ContractType> _allContractTypes = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public ContractTypeUserControlViewModel(ContractTypeService contractTypeService)
    {
        _contractTypeService = contractTypeService;
        LoadContractTypesCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Tip Contract";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<ContractType> _filteredContractTypes = [];

    [ObservableProperty]
    private ContractType? _selectedContractType;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private int? _value;

        [RelayCommand]
    private void ClearSelectedContractType()
    {
        _isUpdatingFromSelection = true;
        
        try
        {
            _hasBeenFiltered = true;
            SelectedContractType = null;

            Name = null;
            Value = null;
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }

        ApplyFilter();
        _hasBeenFiltered = false;
    }

    [RelayCommand]
    public async Task LoadContractTypesAsync()
    {
        IsLoading = true;
        _allContractTypes = await _contractTypeService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedContractTypeChanged(ContractType? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected ContractType changed: Id={value?.Id}, Name={value?.Name}");

        _isUpdatingFromSelection = true;

        try
        {
            if (value is null)
            {
                Name = null;
                Value = null;
            }
            else
            {
                Name = value.Name;
                Value = value.Value;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnNameChanged(string? value) => OnFieldChanged();

    partial void OnValueChanged(int? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedContractType is not null)
            return;

        if (_isUpdatingFromSelection)
            return;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _hasBeenFiltered = true;
        try
        {
                    var filtered = _allContractTypes.Where(c =>
                StartsWith(c.Name, Name) &&
                (Value == null || c.Value == Value) ||
                c.Equals(SelectedContractType)
            ).OrderByDescending(x => x.Id).ToList();

            FilteredContractTypes = new ObservableCollection<ContractType>(filtered);
        }
        finally
        {
            _hasBeenFiltered = false;
        }
    }

    private static bool StartsWith(string? value, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Numele tipului de contract este obligatoriu.");
        if (Value == null || Value <= 0)
            errors.Add("Valoarea trebuie sa fie mai mare ca 0.");

        return errors;
    }

    public ContractType GetSelectedContractType()
    {
        if (SelectedContractType is null)
        {
            return new ContractType
            {
                Name = Name!,
                Value = Value ?? 0
            };
        }

        return SelectedContractType;
    }

    public async Task<ContractType> SaveContractTypeAsync()
    {
        ContractType savedType;
        if (SelectedContractType is null)
        {
            var existing = await _contractTypeService.FindByName(Name!);
            if (existing != null)
            {
                savedType = existing;
                savedType.Value = Value ?? 0;
                await _contractTypeService.Update(savedType);
            }
            else
            {
                savedType = GetSelectedContractType();
                await _contractTypeService.Add(savedType);
            }
        }
        else
        {
            savedType = SelectedContractType;
            savedType.Name = Name!;
            savedType.Value = Value ?? 0;
            await _contractTypeService.Update(savedType);
        }

        await _contractTypeService.SubmitChanges();
        await LoadContractTypesAsync();
        SelectedContractType = FilteredContractTypes.FirstOrDefault(c => c.Id == savedType.Id);
        
        return savedType;
    }

    public async Task DeleteContractTypeAsync()
    {
        if (SelectedContractType is null)
            return;

        var selected = SelectedContractType;
        AppLogger.LogInfo($"Deleting ContractType: Id={selected.Id}, Name={selected.Name}");

        await _contractTypeService.Delete(selected);
        await _contractTypeService.SubmitChanges();

        ClearSelectedContractType();
        await LoadContractTypesAsync();
        AppLogger.LogInfo($"ContractType Id={selected.Id} deleted successfully.");
    }

    public void Dispose()
    {
    }
}
