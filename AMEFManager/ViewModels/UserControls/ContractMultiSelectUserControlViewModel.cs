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

public partial class ContractMultiSelectUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly ContractService _contractService;
    private readonly ClientService? _clientService;
    private List<Contract> _allContracts = [];

    public Func<IEnumerable<Contract>, List<string>>? Validator { get; set; }

    public ContractMultiSelectUserControlViewModel(ContractService contractService, ClientService? clientService = null)
    {
        _contractService = contractService;
        _clientService = clientService;
        SelectedContracts = new ObservableCollection<Contract>();
        LoadContractsCommand.Execute(null);
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<Contract> _filteredContracts = [];
    [ObservableProperty] private ObservableCollection<Contract> _selectedContracts;
    
    [ObservableProperty] private Contract? _contractToAdd;

    [ObservableProperty] private ObservableCollection<Client> _availableClients = [];
    [ObservableProperty] private Client? _selectedClientFilter;
    [ObservableProperty] private string? _clientFilter;

    [ObservableProperty] private int? _number;

    [RelayCommand]
    private void ClearClientFilter()
    {
        ClientFilter = null;
        SelectedClientFilter = null;
    }

    [RelayCommand]
    public async Task LoadContractsAsync()
    {
        IsLoading = true;
        _allContracts = await _contractService.FindAll();

        if (_clientService != null)
        {
            var clients = await _clientService.FindAll();
            AvailableClients = new ObservableCollection<Client>(clients.OrderBy(c => c.Name));
        }
        else
        {
            var clients = _allContracts
                .Where(c => c.Client != null)
                .Select(c => c.Client)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderBy(c => c.Name)
                .ToList();
            AvailableClients = new ObservableCollection<Client>(clients);
        }

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
    partial void OnSelectedClientFilterChanged(Client? value) => ApplyFilter();
    partial void OnClientFilterChanged(string? value) => ApplyFilter();

    public void ApplyFilter()
    {
        var filtered = _allContracts.Where(c =>
            (Number == null || c.Number == Number) &&
            (string.IsNullOrWhiteSpace(ClientFilter) || (c.Client != null && (
                c.Client.Name.Contains(ClientFilter, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(c.Client.NationalIdentifier) && c.Client.NationalIdentifier.Contains(ClientFilter, StringComparison.OrdinalIgnoreCase))
            ))) &&
            (SelectedClientFilter == null || c.ClientId == SelectedClientFilter.Id || c.Client?.Id == SelectedClientFilter.Id) &&
            !SelectedContracts.Contains(c)
        ).OrderByDescending(x => x.Number).ToList();

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

    public void Dispose()
    {
    }
}
