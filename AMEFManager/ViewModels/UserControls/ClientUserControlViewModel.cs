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

public partial class ClientUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly ClientService _clientService;
    private readonly AddressService _addressService;
    private readonly PersonService _personService;
    private readonly PropertyChangedEventHandler _addressHandler;
    private readonly PropertyChangedEventHandler _personHandler;
    private List<Client> _allClients = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;
    private bool _disposed;

    public AddressUserControlViewModel AddressUserControlViewModel { get; }
    public PersonUserControlViewModel PersonUserControlViewModel { get; }

    public ClientUserControlViewModel(ClientService clientService, AddressService addressService, PersonService personService, List<Address>? initialAddresses = null)
    {
        _clientService = clientService;
        _addressService = addressService;
        _personService = personService;
        AddressUserControlViewModel = new AddressUserControlViewModel(addressService, initialAddresses)
        {
            HeaderTitle = "Adresă Sediu Social Client"
        };
        PersonUserControlViewModel = new PersonUserControlViewModel(_personService, addressService, initialAddresses)
        {
            HeaderTitle = "Date Reprezentant Legal Client"
        };
        PersonUserControlViewModel.AddressUserControlViewModel.HeaderTitle = "Adresă Domiciliu Reprezentant";
        
        _addressHandler = (s, e) => { if (e.PropertyName == nameof(AddressUserControlViewModel.SelectedAddress) && !_isUpdatingFromSelection) ApplyFilter(); };
        _personHandler = (s, e) => { if (e.PropertyName == nameof(PersonUserControlViewModel.SelectedPerson) && !_isUpdatingFromSelection) ApplyFilter(); };
        AddressUserControlViewModel.PropertyChanged += _addressHandler;
        PersonUserControlViewModel.PropertyChanged += _personHandler;

        LoadClientsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Societate Comercială (Client)";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Client> _filteredClients = [];

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _nationalIdentifier;

    [ObservableProperty]
    private string? _registrationNumber;

    [ObservableProperty]
    private bool _paysTVA;

    [RelayCommand]
    private void ClearSelectedClient()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            _hasBeenFiltered = true;
            SelectedClient = null;

            Name = null;
            NationalIdentifier = null;
            RegistrationNumber = null;
            PaysTVA = false;
            AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
            PersonUserControlViewModel.ClearSelectedPersonCommand.Execute(null);
        }
        finally
        {
            _isUpdatingFromSelection = false;
            ApplyFilter();
            _hasBeenFiltered = false;
        }
    }

    [RelayCommand]
    public async Task LoadClientsAsync()
    {
        IsLoading = true;
        _allClients = await _clientService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedClientChanged(Client? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected Client changed: Id={value?.Id}, Name={value?.Name}");

        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                Name = null;
                NationalIdentifier = null;
                RegistrationNumber = null;
                PaysTVA = false;
                AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
                PersonUserControlViewModel.ClearSelectedPersonCommand.Execute(null);
            }
            else
            {
                Name = value.Name;
                NationalIdentifier = value.NationalIdentifier;
                RegistrationNumber = value.RegistrationNumber;
                PaysTVA = value.PaysTVA;

                var matchingAddress = AddressUserControlViewModel.FilteredAddresses
                    .FirstOrDefault(a => a.Id == value.AddressId) ?? value.Address;
                AddressUserControlViewModel.SelectedAddress = matchingAddress;

                var matchingPerson = PersonUserControlViewModel.FilteredPersons
                    .FirstOrDefault(p => p.Id == value.PersonId) ?? value.Person;
                PersonUserControlViewModel.SelectedPerson = matchingPerson;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnNameChanged(string? value) => OnFieldChanged();
    partial void OnNationalIdentifierChanged(string? value) => OnFieldChanged();
    partial void OnRegistrationNumberChanged(string? value) => OnFieldChanged();
    partial void OnPaysTVAChanged(bool value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedClient is not null)
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
            var selectedAddress = AddressUserControlViewModel.SelectedAddress;
            var selectedPerson = PersonUserControlViewModel.SelectedPerson;

            var filtered = _allClients.Where(c =>
                StartsWith(c.Name, Name) &&
                (StartsWith(c.NationalIdentifier, NationalIdentifier) || StartsWith(c.GetFormattedCui(), NationalIdentifier)) &&
                StartsWith(c.RegistrationNumber, RegistrationNumber) &&
                (selectedAddress == null || c.AddressId == selectedAddress.Id) &&
                (selectedPerson == null || c.PersonId == selectedPerson.Id) ||
                c.Equals(SelectedClient)
            ).OrderByDescending(x => x.Id).ToList();

            FilteredClients = new ObservableCollection<Client>(filtered);
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
            errors.Add("Numele firmei este obligatoriu.");
        if (string.IsNullOrWhiteSpace(NationalIdentifier))
            errors.Add("CUI-ul este obligatoriu.");
        if (string.IsNullOrWhiteSpace(RegistrationNumber))
            errors.Add("CUI/CIF este obligatoriu.");

        errors.AddRange(PersonUserControlViewModel.Validate());
        errors.AddRange(AddressUserControlViewModel.Validate()); return errors;
    }

    public Client GetSelectedClient()
    {
        if (SelectedClient is null)
        {
            return new Client
            {
                Name = Name!,
                NationalIdentifier = NationalIdentifier!,
                RegistrationNumber = RegistrationNumber!,
                PaysTVA = PaysTVA
            };
        }

        return SelectedClient;
    }

    public async Task<Client> SaveClientAsync()
    {
        var address = await AddressUserControlViewModel.SaveAddressAsync();
        var savedPerson = await PersonUserControlViewModel.SavePersonAsync();

        Client savedClient;
        if (SelectedClient is null)
        {
            var existing = await _clientService.FindByNationalIdentifier(NationalIdentifier!) ??
                           await _clientService.FindByRegistrationNumber(RegistrationNumber!);
            if (existing != null)
            {
                savedClient = existing;
                savedClient.Name = Name!;
                savedClient.NationalIdentifier = NationalIdentifier!;
                savedClient.RegistrationNumber = RegistrationNumber!;
                savedClient.PaysTVA = PaysTVA;
                savedClient.Address = address;
                savedClient.AddressId = address.Id;
                savedClient.Person = savedPerson;
                savedClient.PersonId = savedPerson.Id;
                await _clientService.Update(savedClient);
            }
            else
            {
                savedClient = GetSelectedClient();
                savedClient.Address = address;
                savedClient.AddressId = address.Id;
                savedClient.Person = savedPerson;
                savedClient.PersonId = savedPerson.Id;
                await _clientService.Add(savedClient);
            }
        }
        else
        {
            savedClient = SelectedClient;
            savedClient.Name = Name!;
            savedClient.NationalIdentifier = NationalIdentifier!;
            savedClient.RegistrationNumber = RegistrationNumber!;
            savedClient.PaysTVA = PaysTVA;
            savedClient.Address = address;
            savedClient.AddressId = address.Id;
            savedClient.Person = savedPerson;
            savedClient.PersonId = savedPerson.Id;
            await _clientService.Update(savedClient);
        }

        await _clientService.SubmitChanges();
        await LoadClientsAsync();
        SelectedClient = FilteredClients.FirstOrDefault(c => c.Id == savedClient.Id);
        AppLogger.LogInfo($"Successfully saved Client: Id={savedClient.Id}, Name={savedClient.Name}, NationalIdentifier={savedClient.NationalIdentifier}");

        return savedClient;
    }

    public async Task DeleteClientAsync()
    {
        if (SelectedClient is null) return;

        var toDelete = SelectedClient;
        AppLogger.LogInfo($"Deleting Client: Id={toDelete.Id}, Name={toDelete.Name}, NationalIdentifier={toDelete.NationalIdentifier}");

        var person = toDelete.Person ?? (toDelete.PersonId > 0 ? await _personService.FindById(toDelete.PersonId) : null);
        var address = toDelete.Address ?? (toDelete.AddressId > 0 ? await _addressService.FindById(toDelete.AddressId) : null);
        var personAddress = person?.Address ?? (person?.AddressId > 0 ? await _addressService.FindById(person.AddressId) : null);

        await _clientService.Delete(toDelete);

        try
        {
            if (person != null) await _personService.Delete(person);
            if (address != null) await _addressService.Delete(address);
            if (personAddress != null && (address == null || personAddress.Id != address.Id)) await _addressService.Delete(personAddress);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Cascading delete associated records for Client Id={toDelete.Id} threw: {ex.Message}");
        }

        await _clientService.SubmitChanges();
        ClearSelectedClient();
        await LoadClientsAsync();
        await AddressUserControlViewModel.LoadAddressesAsync();
        await PersonUserControlViewModel.LoadPersonsAsync();
        AppLogger.LogInfo($"Client Id={toDelete.Id} deleted successfully.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AddressUserControlViewModel.PropertyChanged -= _addressHandler;
        PersonUserControlViewModel.PropertyChanged -= _personHandler;

        AddressUserControlViewModel.Dispose();
        PersonUserControlViewModel.Dispose();
    }
}
