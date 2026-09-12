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

public partial class ContractUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly ContractService _contractService;
    private readonly PropertyChangedEventHandler _clientHandler;
    private readonly PropertyChangedEventHandler _contractTypeHandler;
    private List<Contract> _allContracts = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;
    private bool _disposed;

    public ContractTypeUserControlViewModel ContractTypeUserControlViewModel { get; }
    public ClientUserControlViewModel ClientUserControlViewModel { get; }

    public ContractUserControlViewModel(ContractService contractService, ContractTypeService contractTypeService, ClientService clientService, AddressService addressService, PersonService personService, List<Address>? initialAddresses = null)
    {
        _contractService = contractService;
        ContractTypeUserControlViewModel = new ContractTypeUserControlViewModel(contractTypeService)
        {
            HeaderTitle = "Tip Contract"
        };
        ClientUserControlViewModel = new ClientUserControlViewModel(clientService, addressService, personService, initialAddresses)
        {
            HeaderTitle = "Client Beneficiar Contract"
        };
        ClientUserControlViewModel.AddressUserControlViewModel.HeaderTitle = "Adresă Sediu Social Client";
        ClientUserControlViewModel.PersonUserControlViewModel.HeaderTitle = "Date Reprezentant Legal Client";
        ClientUserControlViewModel.PersonUserControlViewModel.AddressUserControlViewModel.HeaderTitle = "Adresă Domiciliu Reprezentant";
        
        _clientHandler = (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };
        _contractTypeHandler = (s, e) => { if (e.PropertyName == nameof(ContractTypeUserControlViewModel.SelectedContractType) && !_isUpdatingFromSelection) ApplyFilter(); };
        ClientUserControlViewModel.PropertyChanged += _clientHandler;
        ContractTypeUserControlViewModel.PropertyChanged += _contractTypeHandler;

        LoadContractsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Contract de Service";

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

        private int GetNextContractNumber()
    {
        return _allContracts.Count > 0 ? _allContracts.Max(c => c.Number) + 1 : 1;
    }

    [RelayCommand]
    private void ClearSelectedContract()
    {
        _isUpdatingFromSelection = true;
        
        try
        {
            _hasBeenFiltered = true;
            SelectedContract = null;

            Number = GetNextContractNumber();
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
        if (SelectedContract is null && (Number is null || Number == 0))
        {
            Number = GetNextContractNumber();
        }
        IsLoading = false;
    }

    partial void OnSelectedContractChanged(Contract? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected Contract changed: Id={value?.Id}, Number={value?.Number}");

        _isUpdatingFromSelection = true;

        try
        {
            if (value is null)
            {
                Number = GetNextContractNumber();
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
                Date = DateHelper.ToDateTimeOffset(value.Date);
                IsActive = value.IsActive;
                ValidUntil = DateHelper.ToDateTimeOffset(value.ValidUntil);

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
            var selectedType = ContractTypeUserControlViewModel.SelectedContractType;

            var filtered = _allContracts.Where(c =>
                (selectedClient == null || c.ClientId == selectedClient.Id) &&
                (selectedType == null || c.ContractTypeId == selectedType.Id) ||
                c.Equals(SelectedContract)
            ).OrderByDescending(x => x.Number).ToList();

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
                ValidUntil = DateOnly.FromDateTime(ValidUntil!.Value.DateTime)
            };
        }

        return SelectedContract;
    }

    public async Task<Contract> SaveContractAsync()
    {
        var clientVm = ClientUserControlViewModel;
        var savedClient = await clientVm.SaveClientAsync();

        var typeVm = ContractTypeUserControlViewModel;
        var savedType = await typeVm.SaveContractTypeAsync();

        Contract savedContract;
        if (SelectedContract is null)
        {
            var existing = await _contractService.FindByNumberAndClientAsync(Number ?? 0, savedClient.Id);
            if (existing != null)
            {
                savedContract = existing;
                savedContract.Date = DateOnly.FromDateTime(Date!.Value.DateTime);
                savedContract.IsActive = IsActive;
                savedContract.ValidUntil = DateOnly.FromDateTime(ValidUntil!.Value.DateTime);
                savedContract.Type = savedType;
                savedContract.ContractTypeId = savedType.Id;
                savedContract.Client = savedClient;
                savedContract.ClientId = savedClient.Id;
                await _contractService.Update(savedContract);
            }
            else
            {
                savedContract = GetSelectedContract();
                savedContract.Type = savedType;
                savedContract.ContractTypeId = savedType.Id;
                savedContract.Client = savedClient;
                savedContract.ClientId = savedClient.Id;
                await _contractService.Add(savedContract);
            }
        }
        else
        {
            savedContract = SelectedContract;
            savedContract.Number = Number ?? 0;
            savedContract.Date = DateOnly.FromDateTime(Date!.Value.DateTime);
            savedContract.IsActive = IsActive;
            savedContract.ValidUntil = DateOnly.FromDateTime(ValidUntil!.Value.DateTime);
            savedContract.Type = savedType;
            savedContract.ContractTypeId = savedType.Id;
            savedContract.Client = savedClient;
            savedContract.ClientId = savedClient.Id;
            await _contractService.Update(savedContract);
        }

        await _contractService.SubmitChanges();
        await LoadContractsAsync();
        SelectedContract = FilteredContracts.FirstOrDefault(c => c.Id == savedContract.Id);
        
        return savedContract;
    }

    public async Task DeleteContractAsync()
    {
        if (SelectedContract is null) return;

        var toDelete = SelectedContract;
        AppLogger.LogInfo($"Deleting Contract: Id={toDelete.Id}, Number={toDelete.Number}");

        if (await _contractService.IsContractInUseAsync(toDelete.Id))
        {
            throw new InvalidOperationException("Contractul nu poate fi șters deoarece are aparate AMEF sau acte adiționale asociate.");
        }

        await _contractService.Delete(toDelete);
        await _contractService.SubmitChanges();
        ClearSelectedContract();
        await LoadContractsAsync();
        AppLogger.LogInfo($"Contract Id={toDelete.Id} deleted successfully.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ClientUserControlViewModel.PropertyChanged -= _clientHandler;
        ContractTypeUserControlViewModel.PropertyChanged -= _contractTypeHandler;

        ContractTypeUserControlViewModel.Dispose();
        ClientUserControlViewModel.Dispose();
    }
}
