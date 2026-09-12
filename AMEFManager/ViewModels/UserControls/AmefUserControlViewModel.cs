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
        ContractUserControlViewModel = new ContractUserControlViewModel(contractService, contractTypeService, clientService, addressService, personService)
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
        _addressHandler = (s, e) => { if (e.PropertyName == nameof(AddressUserControlViewModel.SelectedAddress) && !_isUpdatingFromSelection) ApplyFilter(); };

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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUnassignContract))]
    private Amef? _selectedAmef;

    public bool CanUnassignContract => SelectedAmef != null && (SelectedAmef.ContractId.HasValue || SelectedAmef.Contract != null);

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
        OnPropertyChanged(nameof(CanUnassignContract));
        UnassignContractCommand.NotifyCanExecuteChanged();

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
                FiscalizationDate = DateHelper.ToDateTimeOffset(value.FiscalizationDate);
                ConnectionMethod = value.ConnectionMethod;
                ConnectionExpirationDate = DateHelper.ToDateTimeOffset(value.ConnectionExpirationDate);
                ServicePassword = value.ServicePassword;

                BillUserControlViewModel.SelectedBill = value.BillId.HasValue 
                    ? BillUserControlViewModel.FilteredBills.FirstOrDefault(b => b.Id == value.BillId.Value) ?? value.Bill
                    : null;
                AddressUserControlViewModel.SelectedAddress = value.AddressId.HasValue 
                    ? AddressUserControlViewModel.FilteredAddresses.FirstOrDefault(a => a.Id == value.AddressId.Value) ?? value.Address
                    : null;
                AuthorizationUserControlViewModel.SelectedAuthorization = AuthorizationUserControlViewModel.FilteredAuthorizations.FirstOrDefault(a => a.Id == value.AuthorizationId) ?? value.Authorization;
                ContractUserControlViewModel.SelectedContract = value.ContractId.HasValue 
                    ? ContractUserControlViewModel.FilteredContracts.FirstOrDefault(c => c.Id == value.ContractId.Value) ?? value.Contract
                    : null;
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
            ).OrderByDescending(x => x.Id).ToList();

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
        errors.AddRange(AuthorizationUserControlViewModel.Validate());

        return errors;
    }

    public Amef GetSelectedAmef()
    {
        if (SelectedAmef is null)
        {
            return new Amef
            {
                Series = Series!,
                NUI = Nui,
                FiscalCity = FiscalCity,
                FiscalizationDate = DateHelper.ToDateOnly(FiscalizationDate),
                ConnectionMethod = ConnectionMethod,
                ConnectionExpirationDate = DateHelper.ToDateOnly(ConnectionExpirationDate),
                ServicePassword = ServicePassword,
                AuthorizationId = AuthorizationUserControlViewModel.SelectedAuthorization?.Id ?? 0
            };
        }
        return SelectedAmef;
    }

    public async Task<Amef> SaveAmefAsync()
    {
        var savedAuth = await AuthorizationUserControlViewModel.SaveAuthorizationAsync();

        Address? address = null;
        if (AddressUserControlViewModel.SelectedAddress != null || AddressUserControlViewModel.Validate().Count == 0)
        {
            address = await AddressUserControlViewModel.SaveAddressAsync();
        }

        Bill? savedBill = null;
        if (BillUserControlViewModel.SelectedBill != null || BillUserControlViewModel.Validate().Count == 0)
        {
            savedBill = await BillUserControlViewModel.SaveBillAsync();
        }

        Contract? savedContract = null;
        if (ContractUserControlViewModel.SelectedContract != null || ContractUserControlViewModel.Validate().Count == 0)
        {
            savedContract = await ContractUserControlViewModel.SaveContractAsync();
        }

        Amef savedAmef;
        if (SelectedAmef is null)
        {
            Amef? existing = null;
            if (!string.IsNullOrWhiteSpace(Nui))
            {
                existing = await _amefService.FindByNui(Nui);
            }
            if (existing == null && !string.IsNullOrWhiteSpace(Series))
            {
                existing = await _amefService.FindBySeries(Series);
            }

            if (existing != null)
            {
                savedAmef = existing;
                savedAmef.Series = Series!;
                savedAmef.NUI = Nui;
                savedAmef.FiscalCity = FiscalCity;
                savedAmef.FiscalizationDate = DateHelper.ToDateOnly(FiscalizationDate);
                savedAmef.ConnectionMethod = ConnectionMethod;
                savedAmef.ConnectionExpirationDate = DateHelper.ToDateOnly(ConnectionExpirationDate);
                savedAmef.ServicePassword = ServicePassword;
                savedAmef.Address = address;
                savedAmef.AddressId = address?.Id;
                savedAmef.Bill = savedBill;
                savedAmef.BillId = savedBill?.Id;
                savedAmef.Authorization = savedAuth;
                savedAmef.AuthorizationId = savedAuth.Id;
                savedAmef.Contract = savedContract;
                savedAmef.ContractId = savedContract?.Id;
                await _amefService.Update(savedAmef);
            }
            else
            {
                savedAmef = GetSelectedAmef();
                savedAmef.Address = address;
                savedAmef.AddressId = address?.Id;
                savedAmef.Bill = savedBill;
                savedAmef.BillId = savedBill?.Id;
                savedAmef.Authorization = savedAuth;
                savedAmef.AuthorizationId = savedAuth.Id;
                savedAmef.Contract = savedContract;
                savedAmef.ContractId = savedContract?.Id;
                await _amefService.Add(savedAmef);
            }
        }
        else
        {
            savedAmef = SelectedAmef;
            savedAmef.Series = Series!;
            savedAmef.NUI = Nui;
            savedAmef.FiscalCity = FiscalCity;
            savedAmef.FiscalizationDate = DateHelper.ToDateOnly(FiscalizationDate);
            savedAmef.ConnectionMethod = ConnectionMethod;
            savedAmef.ConnectionExpirationDate = DateHelper.ToDateOnly(ConnectionExpirationDate);
            savedAmef.ServicePassword = ServicePassword;
            savedAmef.Address = address;
            savedAmef.AddressId = address?.Id;
            savedAmef.Bill = savedBill;
            savedAmef.BillId = savedBill?.Id;
            savedAmef.Authorization = savedAuth;
            savedAmef.AuthorizationId = savedAuth.Id;
            savedAmef.Contract = savedContract;
            savedAmef.ContractId = savedContract?.Id;
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

        if (await _amefService.IsAmefInUseAsync(toDelete.Id))
        {
            throw new InvalidOperationException("Aparatul AMEF nu poate fi șters deoarece este inclus în documente de predare, sigilare sau declarații C802.");
        }

        await _amefService.Delete(toDelete);
        await _amefService.SubmitChanges();

        ClearSelectedAmef();
        await LoadAmefsAsync();
        AppLogger.LogInfo($"AMEF Id={toDelete.Id} deleted successfully.");
    }

    [RelayCommand(CanExecute = nameof(CanUnassignContract))]
    public async Task UnassignContractAsync()
    {
        if (SelectedAmef is null) return;
        if (!SelectedAmef.ContractId.HasValue && SelectedAmef.Contract == null) return;

        var toUpdate = SelectedAmef;
        AppLogger.LogInfo($"Unassigning AMEF Id={toUpdate.Id}, Series={toUpdate.Series} from ContractId={toUpdate.ContractId}");

        if (toUpdate.Contract?.Amefs != null)
        {
            toUpdate.Contract.Amefs.Remove(toUpdate);
        }
        toUpdate.ContractId = null;
        toUpdate.Contract = null;

        await _amefService.Update(toUpdate);
        await _amefService.SubmitChanges();

        ContractUserControlViewModel.ClearSelectedContractCommand.Execute(null);

        var amefId = toUpdate.Id;
        await LoadAmefsAsync();

        var reloaded = FilteredAmefs.FirstOrDefault(a => a.Id == amefId) ?? _allAmefs.FirstOrDefault(a => a.Id == amefId);
        SelectedAmef = null;
        SelectedAmef = reloaded;

        await ContractUserControlViewModel.LoadContractsAsync();
        AppLogger.LogInfo($"AMEF Id={amefId} successfully unassigned from contract.");
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
