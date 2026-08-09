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

public partial class ContractUserControlViewModel : ViewModelBase
{
    private readonly ContractService _contractService;
    private List<Contract> _allContracts = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public ContractTypeUserControlViewModel ContractTypeUserControlViewModel { get; }
    public ClientUserControlViewModel ClientUserControlViewModel { get; }

    public ContractUserControlViewModel(ContractService contractService, ContractTypeService contractTypeService, ClientService clientService, AddressService addressService, PersonService personService)
    {
        _contractService = contractService;
        ContractTypeUserControlViewModel = new ContractTypeUserControlViewModel(contractTypeService);
        ClientUserControlViewModel = new ClientUserControlViewModel(clientService, addressService, personService);
        
        ClientUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };

        LoadContractsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Contract> _filteredContracts = [];

    [ObservableProperty]
    private Contract? _selectedContract;

    [ObservableProperty]
    private int? _number;

    [ObservableProperty]
    private DateTimeOffset? _date;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private DateTimeOffset? _validUntil;

    [ObservableProperty]
    private ObservableCollection<Amef> _amefs = [];

        [RelayCommand]
    private void ClearSelectedContract()
    {
        _isUpdatingFromSelection = true;
        
        try
        {
            // Disable the automatic OnSelectedContractChanged logic
            _hasBeenFiltered = true;
            SelectedContract = null;

            // Manually clear all fields
            Number = null;
            Date = null;
            IsActive = true;
            ValidUntil = null;
            ContractTypeUserControlViewModel.ClearSelectedContractTypeCommand.Execute(null);
            ClientUserControlViewModel.ClearSelectedClientCommand.Execute(null);
            Amefs.Clear();
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }

        ApplyFilter();
        _hasBeenFiltered = false;
    }

    [RelayCommand]
    public async Task LoadContractsAsync()
    {
        IsLoading = true;
        _allContracts = await _contractService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedContractChanged(Contract? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        Debug.Print("Selected contract changed");

        _isUpdatingFromSelection = true;

        try
        {
            if (value is null)
            {
                Number = null;
                Date = null;
                IsActive = true;
                ValidUntil = null;
                ContractTypeUserControlViewModel.ClearSelectedContractTypeCommand.Execute(null);
                ClientUserControlViewModel.ClearSelectedClientCommand.Execute(null);
                Amefs.Clear();
            }
            else
            {
                Number = value.Number;
                Date = new DateTimeOffset(value.Date.ToDateTime(TimeOnly.MinValue));
                IsActive = value.IsActive;
                ValidUntil = new DateTimeOffset(value.ValidUntil);

                var matchingType = ContractTypeUserControlViewModel.FilteredContractTypes
                    .FirstOrDefault(t => t.Id == value.ContractTypeId);
                ContractTypeUserControlViewModel.SelectedContractType = matchingType;
                
                var matchingClient = ClientUserControlViewModel.FilteredClients
                    .FirstOrDefault(c => c.Id == value.ClientId);
                ClientUserControlViewModel.SelectedClient = matchingClient;
                
                Amefs = new ObservableCollection<Amef>(value.Amefs);
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnNumberChanged(int? value) => OnFieldChanged();
    partial void OnDateChanged(DateTimeOffset? value) => OnFieldChanged();
    partial void OnValidUntilChanged(DateTimeOffset? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedContract is not null)
            return;

        if (_isUpdatingFromSelection)
            return;

        ApplyFilter();
    }

    public void ApplyFilter()
    {
        _hasBeenFiltered = true;
        try
        {
                    var selectedClient = ClientUserControlViewModel.SelectedClient;

            var filtered = _allContracts.Where(c =>
                (Number == null || c.Number == Number) &&
                (selectedClient == null || c.ClientId == selectedClient.Id) ||
                c.Equals(SelectedContract)
            ).OrderBy(x => x.Id).ToList();

            FilteredContracts = new ObservableCollection<Contract>(filtered);
        }
        finally
        {
            _hasBeenFiltered = false;
        }
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Number == null || Number <= 0)
            errors.Add("Numarul contractului este obligatoriu.");
        if (Date is null)
            errors.Add("Data contractului este obligatorie.");
        if (ValidUntil is null)
            errors.Add("Data expirarii este obligatorie.");
        errors.AddRange(ContractTypeUserControlViewModel.Validate());
        errors.AddRange(ClientUserControlViewModel.Validate());

        return errors;
    }

    public Contract GetSelectedContract()
    {
        if (SelectedContract is null)
        {
            return new Contract
            {
                Number = Number ?? 0,
                Date = DateOnly.FromDateTime(Date!.Value.DateTime),
                IsActive = IsActive,
                ValidUntil = ValidUntil!.Value.DateTime
            };
        }

        return SelectedContract;
    }
}
