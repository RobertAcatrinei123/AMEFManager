using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.UserControls;

public partial class ContractTypeUserControlViewModel : ViewModelBase
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

        Debug.Print("Selected contract type changed");

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
            ).OrderBy(x => x.Id).ToList();

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
}
