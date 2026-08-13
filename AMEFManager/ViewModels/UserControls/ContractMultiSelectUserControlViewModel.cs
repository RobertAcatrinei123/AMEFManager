using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.UserControls;

public partial class ContractMultiSelectUserControlViewModel : ViewModelBase
{
    private readonly ContractService _contractService;
    private List<Contract> _allContracts = [];

    public Func<IEnumerable<Contract>, List<string>>? Validator { get; set; }

    public ContractMultiSelectUserControlViewModel(ContractService contractService)
    {
        _contractService = contractService;
        SelectedContracts = new ObservableCollection<Contract>();
        LoadContractsCommand.Execute(null);
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<Contract> _filteredContracts = [];
    [ObservableProperty] private ObservableCollection<Contract> _selectedContracts;
    
    [ObservableProperty] private Contract? _contractToAdd;

    [ObservableProperty] private int? _number;

    [RelayCommand]
    public async Task LoadContractsAsync()
    {
        IsLoading = true;
        _allContracts = await _contractService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnContractToAddChanged(Contract? value)
    {
        if (value is not null)
        {
            if (!SelectedContracts.Contains(value))
            {
                SelectedContracts.Add(value);
            }
            
            ContractToAdd = null;
            Number = null;
            
            ApplyFilter();
        }
    }

    [RelayCommand]
    private void RemoveContract(Contract contract)
    {
        if (contract is not null && SelectedContracts.Contains(contract))
        {
            SelectedContracts.Remove(contract);
            ApplyFilter();
        }
    }

    partial void OnNumberChanged(int? value) => ApplyFilter();

    public void ApplyFilter()
    {
        var filtered = _allContracts.Where(c =>
            (Number == null || c.Number == Number) &&
            !SelectedContracts.Contains(c)
        ).OrderBy(x => x.Number).ToList();

        FilteredContracts = new ObservableCollection<Contract>(filtered);
    }

    public List<string> Validate()
    {
        if (Validator != null)
        {
            return Validator(SelectedContracts);
        }
        return [];
    }
}
