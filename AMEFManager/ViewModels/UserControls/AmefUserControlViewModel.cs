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

public partial class AmefUserControlViewModel : ViewModelBase
{
    private readonly AmefService _amefService;
    private List<Amef> _allAmefs = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public BillUserControlViewModel BillUserControlViewModel { get; }
    public AddressUserControlViewModel AddressUserControlViewModel { get; }
    public AuthorizationUserControlViewModel AuthorizationUserControlViewModel { get; }
    public ContractUserControlViewModel ContractUserControlViewModel { get; }

    public AmefUserControlViewModel(
        AmefService amefService,
        BillService billService,
        AddressService addressService,
        AuthorizationService authorizationService,
        ContractService contractService,
        ContractTypeService contractTypeService,
        ClientService clientService,
        PersonService personService)
    {
        _amefService = amefService;
        
        BillUserControlViewModel = new BillUserControlViewModel(billService);
        AddressUserControlViewModel = new AddressUserControlViewModel(addressService);
        AuthorizationUserControlViewModel = new AuthorizationUserControlViewModel(authorizationService);
        ContractUserControlViewModel = new ContractUserControlViewModel(contractService, contractTypeService, clientService, addressService, personService);
        
        ContractUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract) && !_isUpdatingFromSelection) ApplyFilter(); };
        ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };

        LoadAmefsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<Amef> _filteredAmefs = [];
    [ObservableProperty] private Amef? _selectedAmef;

    [ObservableProperty] private string? _model;
    [ObservableProperty] private string? _series;
    [ObservableProperty] private string? _nui;
    [ObservableProperty] private string? _fiscalCity;
    [ObservableProperty] private DateTimeOffset? _fiscalizationDate;
    [ObservableProperty] private string? _connectionMethod;
    [ObservableProperty] private DateTimeOffset? _connectionExpirationDate;

    [RelayCommand]
    private void ClearSelectedAmef()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            // Disable the automatic OnSelectedAmefChanged logic
            _hasBeenFiltered = true;
            SelectedAmef = null;

            // Manually clear all fields
            Model = null;
            Series = null;
            Nui = null;
            FiscalCity = null;
            FiscalizationDate = null;
            ConnectionMethod = null;
            ConnectionExpirationDate = null;
            
            BillUserControlViewModel.ClearSelectedBillCommand.Execute(null);
            AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
            AuthorizationUserControlViewModel.ClearSelectedAuthorizationCommand.Execute(null);
            ContractUserControlViewModel.ClearSelectedContractCommand.Execute(null);
        }
        finally
        {
            _isUpdatingFromSelection = false;
            ApplyFilter();
            _hasBeenFiltered = false;
        }
    }

    [RelayCommand]
    public async Task LoadAmefsAsync()
    {
        IsLoading = true;
        _allAmefs = await _amefService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedAmefChanged(Amef? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        Debug.Print("Selected amef changed");
        
        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                Model = null;
                Series = null;
                Nui = null;
                FiscalCity = null;
                FiscalizationDate = null;
                ConnectionMethod = null;
                ConnectionExpirationDate = null;
                
                BillUserControlViewModel.ClearSelectedBillCommand.Execute(null);
                AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
                AuthorizationUserControlViewModel.ClearSelectedAuthorizationCommand.Execute(null);
                ContractUserControlViewModel.ClearSelectedContractCommand.Execute(null);
            }
            else
            {
                Model = value.Model;
                Series = value.Series;
                Nui = value.NUI;
                FiscalCity = value.FiscalCity;
                FiscalizationDate = new DateTimeOffset(value.FiscalizationDate.ToDateTime(TimeOnly.MinValue));
                ConnectionMethod = value.ConnectionMethod;
                ConnectionExpirationDate = value.ConnectionExpirationDate.HasValue 
                    ? new DateTimeOffset(value.ConnectionExpirationDate.Value.ToDateTime(TimeOnly.MinValue)) 
                    : null;

                BillUserControlViewModel.SelectedBill = BillUserControlViewModel.FilteredBills.FirstOrDefault(b => b.Id == value.BillId);
                AddressUserControlViewModel.SelectedAddress = AddressUserControlViewModel.FilteredAddresses.FirstOrDefault(a => a.Id == value.AddressId);
                AuthorizationUserControlViewModel.SelectedAuthorization = AuthorizationUserControlViewModel.FilteredAuthorizations.FirstOrDefault(a => a.Id == value.AuthorizationId);
                ContractUserControlViewModel.SelectedContract = ContractUserControlViewModel.FilteredContracts.FirstOrDefault(c => c.Id == value.ContractId);
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnFiscalCityChanged(string? value) => OnFieldChanged();
    partial void OnFiscalizationDateChanged(DateTimeOffset? value) => OnFieldChanged();
    partial void OnConnectionMethodChanged(string? value) => OnFieldChanged();
    partial void OnConnectionExpirationDateChanged(DateTimeOffset? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedAmef is not null) return;
        if (_isUpdatingFromSelection) return;
        ApplyFilter();
    }

    partial void OnModelChanged(string? value) => OnFieldChanged();
    partial void OnSeriesChanged(string? value) => OnFieldChanged();
    partial void OnNuiChanged(string? value) => OnFieldChanged();

    public void ApplyFilter()
    {
        _hasBeenFiltered = true;
        try
        {
                    var selectedContract = ContractUserControlViewModel.SelectedContract;
            var selectedClient = ContractUserControlViewModel.ClientUserControlViewModel.SelectedClient;

            var filtered = _allAmefs.Where(a =>
                StartsWith(a.Model, Model) &&
                StartsWith(a.Series, Series) &&
                StartsWith(a.NUI, Nui) &&
                (selectedContract == null || a.ContractId == selectedContract.Id) &&
                (selectedClient == null || a.Contract?.ClientId == selectedClient.Id) ||
                a.Equals(SelectedAmef)
            ).OrderBy(x => x.Id).ToList();

            FilteredAmefs = new ObservableCollection<Amef>(filtered);
        }
        finally
        {
            _hasBeenFiltered = false;
        }
    }

    private static bool StartsWith(string? value, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Model)) errors.Add("Modelul AMEF este obligatoriu.");
        if (string.IsNullOrWhiteSpace(Series)) errors.Add("Seria este obligatorie.");
        if (string.IsNullOrWhiteSpace(Nui)) errors.Add("NUI este obligatoriu.");
        if (string.IsNullOrWhiteSpace(FiscalCity)) errors.Add("Orasul fiscal este obligatoriu.");
        if (FiscalizationDate is null) errors.Add("Data fiscalizarii este obligatorie.");

        errors.AddRange(ContractUserControlViewModel.Validate());
        errors.AddRange(AuthorizationUserControlViewModel.Validate());
        errors.AddRange(BillUserControlViewModel.Validate());
        errors.AddRange(AddressUserControlViewModel.Validate());

        return errors;
    }

    public Amef GetSelectedAmef()
    {
        if (SelectedAmef is null)
        {
            return new Amef
            {
                Model = Model!,
                Series = Series!,
                NUI = Nui!,
                FiscalCity = FiscalCity!,
                FiscalizationDate = DateOnly.FromDateTime(FiscalizationDate!.Value.DateTime),
                ConnectionMethod = ConnectionMethod,
                ConnectionExpirationDate = ConnectionExpirationDate.HasValue ? DateOnly.FromDateTime(ConnectionExpirationDate.Value.DateTime) : null
            };
        }
        return SelectedAmef;
    }
}
