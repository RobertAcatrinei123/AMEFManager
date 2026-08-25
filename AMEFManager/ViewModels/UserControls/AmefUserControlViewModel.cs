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

using System.ComponentModel;

namespace AMEFManager.ViewModels.UserControls;

public partial class AmefUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly AmefService _amefService;
    private readonly BillService _billService;
    private readonly AddressService _addressService;
    private readonly AuthorizationService _authorizationService;
    private readonly PropertyChangedEventHandler _contractHandler;
    private readonly PropertyChangedEventHandler _clientHandler;
    private readonly PropertyChangedEventHandler _contractTypeHandler;
    private readonly PropertyChangedEventHandler _billHandler;
    private readonly PropertyChangedEventHandler _authHandler;
    private readonly PropertyChangedEventHandler _addressHandler;
    private List<Amef> _allAmefs = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;
    private bool _disposed;

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
        _billService = billService;
        _addressService = addressService;
        _authorizationService = authorizationService;
        
        BillUserControlViewModel = new BillUserControlViewModel(billService)
        {
            HeaderTitle = "Factură Achiziție AMEF"
        };
        AddressUserControlViewModel = new AddressUserControlViewModel(addressService)
        {
            HeaderTitle = "Adresă Instalare AMEF / Punct de Lucru"
        };
        AuthorizationUserControlViewModel = new AuthorizationUserControlViewModel(authorizationService)
        {
            HeaderTitle = "Autorizație Distribuție AMEF"
        };
        // Pass shared empty initial addresses to child contract/client/person to eliminate 3x duplicate address queries
        ContractUserControlViewModel = new ContractUserControlViewModel(contractService, contractTypeService, clientService, addressService, personService, initialAddresses: [])
        {
            HeaderTitle = "Contract de Service AMEF"
        };
        ContractUserControlViewModel.ContractTypeUserControlViewModel.HeaderTitle = "Tip Contract";
        ContractUserControlViewModel.ClientUserControlViewModel.HeaderTitle = "Client Beneficiar Contract";
        ContractUserControlViewModel.ClientUserControlViewModel.AddressUserControlViewModel.HeaderTitle = "Adresă Sediu Social Client";
        ContractUserControlViewModel.ClientUserControlViewModel.PersonUserControlViewModel.HeaderTitle = "Date Reprezentant Legal Client";
        ContractUserControlViewModel.ClientUserControlViewModel.PersonUserControlViewModel.AddressUserControlViewModel.HeaderTitle = "Adresă Domiciliu Reprezentant";
        
        _contractHandler = (s, e) => { if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract) && !_isUpdatingFromSelection) ApplyFilter(); };
        _clientHandler = (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };
        _contractTypeHandler = (s, e) => { if (e.PropertyName == nameof(ContractTypeUserControlViewModel.SelectedContractType) && !_isUpdatingFromSelection) ApplyFilter(); };
        _billHandler = (s, e) => { if (e.PropertyName == nameof(BillUserControlViewModel.SelectedBill) && !_isUpdatingFromSelection) ApplyFilter(); };
        _authHandler = (s, e) => { if (e.PropertyName == nameof(AuthorizationUserControlViewModel.SelectedAuthorization) && !_isUpdatingFromSelection) ApplyFilter(); };
        _addressHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(AddressUserControlViewModel.SelectedAddress) && !_isUpdatingFromSelection)
            {
                ApplyFilter();
            }
            else if (e.PropertyName == nameof(AddressUserControlViewModel.FilteredAddresses))
            {
                var addrs = AddressUserControlViewModel.FilteredAddresses.ToList();
                ContractUserControlViewModel.ClientUserControlViewModel.AddressUserControlViewModel.SetAddresses(addrs);
                ContractUserControlViewModel.ClientUserControlViewModel.PersonUserControlViewModel.AddressUserControlViewModel.SetAddresses(addrs);
            }
        };

        ContractUserControlViewModel.PropertyChanged += _contractHandler;
        ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged += _clientHandler;
        ContractUserControlViewModel.ContractTypeUserControlViewModel.PropertyChanged += _contractTypeHandler;
        BillUserControlViewModel.PropertyChanged += _billHandler;
        AuthorizationUserControlViewModel.PropertyChanged += _authHandler;
        AddressUserControlViewModel.PropertyChanged += _addressHandler;

        LoadAmefsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty] private string _headerTitle = "Date Tehnice AMEF";
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
    [ObservableProperty] private string? _servicePassword;

    [RelayCommand]
    private void ClearSelectedAmef()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            _hasBeenFiltered = true;
            SelectedAmef = null;

            Model = null;
            Series = null;
            Nui = null;
            FiscalCity = null;
            FiscalizationDate = null;
            ConnectionMethod = null;
            ConnectionExpirationDate = null;
            ServicePassword = null;
            
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
        var addrs = await _addressService.FindAll();
        AddressUserControlViewModel.SetAddresses(addrs);
        ContractUserControlViewModel.ClientUserControlViewModel.AddressUserControlViewModel.SetAddresses(addrs);
        ContractUserControlViewModel.ClientUserControlViewModel.PersonUserControlViewModel.AddressUserControlViewModel.SetAddresses(addrs);
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedAmefChanged(Amef? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected AMEF changed: Id={value?.Id}, NUI={value?.NUI}, Series={value?.Series}");
        
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
                ServicePassword = null;
                
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
                ServicePassword = value.ServicePassword;

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
            var selectedContractType = ContractUserControlViewModel.ContractTypeUserControlViewModel.SelectedContractType;
            var selectedBill = BillUserControlViewModel.SelectedBill;
            var selectedAuth = AuthorizationUserControlViewModel.SelectedAuthorization;
            var selectedAddress = AddressUserControlViewModel.SelectedAddress;

            var filtered = _allAmefs.Where(a =>
                StartsWith(a.Model, Model) &&
                StartsWith(a.Series, Series) &&
                StartsWith(a.NUI, Nui) &&
                (selectedContract == null || a.ContractId == selectedContract.Id) &&
                (selectedClient == null || a.Contract?.ClientId == selectedClient.Id) &&
                (selectedContractType == null || a.Contract?.ContractTypeId == selectedContractType.Id) &&
                (selectedBill == null || a.BillId == selectedBill.Id) &&
                (selectedAuth == null || a.AuthorizationId == selectedAuth.Id) &&
                (selectedAddress == null || a.AddressId == selectedAddress.Id) ||
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
                Series = Series!,
                NUI = Nui!,
                FiscalCity = FiscalCity!,
                FiscalizationDate = DateOnly.FromDateTime(FiscalizationDate!.Value.DateTime),
                ConnectionMethod = ConnectionMethod,
                ConnectionExpirationDate = ConnectionExpirationDate.HasValue ? DateOnly.FromDateTime(ConnectionExpirationDate.Value.DateTime) : null,
                ServicePassword = ServicePassword
            };
        }
        return SelectedAmef;
    }

    public async Task<Amef> SaveAmefAsync()
    {
        var address = await AddressUserControlViewModel.SaveAddressAsync();
        var savedBill = await BillUserControlViewModel.SaveBillAsync();
        var savedAuth = await AuthorizationUserControlViewModel.SaveAuthorizationAsync();
        var savedContract = await ContractUserControlViewModel.SaveContractAsync();

        Amef savedAmef;
        if (SelectedAmef is null)
        {
            var existing = await _amefService.FindByNui(Nui!) ?? await _amefService.FindBySeries(Series!);
            if (existing != null)
            {
                savedAmef = existing;
                savedAmef.Series = Series!;
                savedAmef.NUI = Nui!;
                savedAmef.FiscalCity = FiscalCity!;
                savedAmef.FiscalizationDate = DateOnly.FromDateTime(FiscalizationDate!.Value.DateTime);
                savedAmef.ConnectionMethod = ConnectionMethod;
                savedAmef.ConnectionExpirationDate = ConnectionExpirationDate.HasValue
                    ? DateOnly.FromDateTime(ConnectionExpirationDate.Value.DateTime)
                    : null;
                savedAmef.ServicePassword = ServicePassword;
                savedAmef.Address = address;
                savedAmef.AddressId = address.Id;
                savedAmef.Bill = savedBill;
                savedAmef.BillId = savedBill.Id;
                savedAmef.Authorization = savedAuth;
                savedAmef.AuthorizationId = savedAuth.Id;
                savedAmef.Contract = savedContract;
                savedAmef.ContractId = savedContract.Id;
                await _amefService.Update(savedAmef);
            }
            else
            {
                savedAmef = GetSelectedAmef();
                savedAmef.Address = address;
                savedAmef.AddressId = address.Id;
                savedAmef.Bill = savedBill;
                savedAmef.BillId = savedBill.Id;
                savedAmef.Authorization = savedAuth;
                savedAmef.AuthorizationId = savedAuth.Id;
                savedAmef.Contract = savedContract;
                savedAmef.ContractId = savedContract.Id;
                await _amefService.Add(savedAmef);
            }
        }
        else
        {
            savedAmef = SelectedAmef;
            savedAmef.Series = Series!;
            savedAmef.NUI = Nui!;
            savedAmef.FiscalCity = FiscalCity!;
            savedAmef.FiscalizationDate = DateOnly.FromDateTime(FiscalizationDate!.Value.DateTime);
            savedAmef.ConnectionMethod = ConnectionMethod;
            savedAmef.ConnectionExpirationDate = ConnectionExpirationDate.HasValue
                ? DateOnly.FromDateTime(ConnectionExpirationDate.Value.DateTime)
                : null;
            savedAmef.ServicePassword = ServicePassword;
            savedAmef.Address = address;
            savedAmef.AddressId = address.Id;
            savedAmef.Bill = savedBill;
            savedAmef.BillId = savedBill.Id;
            savedAmef.Authorization = savedAuth;
            savedAmef.AuthorizationId = savedAuth.Id;
            savedAmef.Contract = savedContract;
            savedAmef.ContractId = savedContract.Id;
            await _amefService.Update(savedAmef);
        }

        await _amefService.SubmitChanges();
        await LoadAmefsAsync();
        SelectedAmef = FilteredAmefs.FirstOrDefault(a => a.Id == savedAmef.Id);
        AppLogger.LogInfo($"Successfully saved AMEF: Id={savedAmef.Id}, NUI={savedAmef.NUI}, Series={savedAmef.Series}");

        return savedAmef;
    }

    public async Task DeleteAmefAsync()
    {
        if (SelectedAmef is null) return;

        var toDelete = SelectedAmef;
        AppLogger.LogInfo($"Deleting AMEF: Id={toDelete.Id}, NUI={toDelete.NUI}, Series={toDelete.Series}");

        var address = toDelete.Address ?? (toDelete.AddressId > 0 ? await _addressService.FindById(toDelete.AddressId) : null);
        var bill = toDelete.Bill ?? (toDelete.BillId > 0 ? await _billService.FindById(toDelete.BillId) : null);
        var auth = toDelete.Authorization ?? (toDelete.AuthorizationId > 0 ? await _authorizationService.FindById(toDelete.AuthorizationId) : null);

        await _amefService.Delete(toDelete);

        try
        {
            if (address != null) await _addressService.Delete(address);
            if (bill != null) await _billService.Delete(bill);
            if (auth != null) await _authorizationService.Delete(auth);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Cascading delete associated records for AMEF Id={toDelete.Id} threw: {ex.Message}");
        }

        await _amefService.SubmitChanges();
        ClearSelectedAmef();
        await LoadAmefsAsync();
        await AddressUserControlViewModel.LoadAddressesAsync();
        await BillUserControlViewModel.LoadBillsAsync();
        await AuthorizationUserControlViewModel.LoadAuthorizationsAsync();
        await ContractUserControlViewModel.LoadContractsAsync();
        AppLogger.LogInfo($"AMEF Id={toDelete.Id} deleted successfully.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ContractUserControlViewModel.PropertyChanged -= _contractHandler;
        ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged -= _clientHandler;
        ContractUserControlViewModel.ContractTypeUserControlViewModel.PropertyChanged -= _contractTypeHandler;
        BillUserControlViewModel.PropertyChanged -= _billHandler;
        AuthorizationUserControlViewModel.PropertyChanged -= _authHandler;
        AddressUserControlViewModel.PropertyChanged -= _addressHandler;

        ContractUserControlViewModel.Dispose();
        BillUserControlViewModel.Dispose();
        AuthorizationUserControlViewModel.Dispose();
        AddressUserControlViewModel.Dispose();
    }
}
