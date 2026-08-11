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

public partial class ClientUserControlViewModel : ViewModelBase
{
    private readonly ClientService _clientService;
    private readonly PersonService _personService;
    private List<Client> _allClients = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public AddressUserControlViewModel AddressUserControlViewModel { get; }
    public PersonUserControlViewModel PersonUserControlViewModel { get; }

    public ClientUserControlViewModel(ClientService clientService, AddressService addressService, PersonService personService)
    {
        _clientService = clientService;
        AddressUserControlViewModel = new AddressUserControlViewModel(addressService);
        _personService = personService;
        PersonUserControlViewModel = new PersonUserControlViewModel(_personService, addressService);
        LoadClientsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

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

        Debug.Print("Selected client changed");

        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                Name = null;
                NationalIdentifier = null;
                RegistrationNumber = null;
                AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
                PersonUserControlViewModel.ClearSelectedPersonCommand.Execute(null);
            }
            else
            {
                Name = value.Name;
                NationalIdentifier = value.NationalIdentifier;
                RegistrationNumber = value.RegistrationNumber;

                var matchingAddress = AddressUserControlViewModel.FilteredAddresses
                    .FirstOrDefault(a => a.Id == value.AddressId);
                AddressUserControlViewModel.SelectedAddress = matchingAddress;

                var matchingPerson = PersonUserControlViewModel.FilteredPersons
                    .FirstOrDefault(p => p.Id == value.PersonId);
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
                    var filtered = _allClients.Where(c =>
                StartsWith(c.Name, Name) &&
                StartsWith(c.NationalIdentifier, NationalIdentifier) &&
                StartsWith(c.RegistrationNumber, RegistrationNumber) ||
                c.Equals(SelectedClient)
            ).OrderBy(x => x.Id).ToList();

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
                RegistrationNumber = RegistrationNumber!
            };
        }

        return SelectedClient;
    }

    public async Task<Client> SaveClientAsync()
    {
        var address = await AddressUserControlViewModel.SaveAddressAsync();

        var personAddressVm = PersonUserControlViewModel.AddressUserControlViewModel;
        var personAddress = await personAddressVm.SaveAddressAsync();

        Person savedPerson;
        if (PersonUserControlViewModel.SelectedPerson is null)
        {
            var existing = await _personService.FindByCnp(PersonUserControlViewModel.Cnp!) ?? 
                           await _personService.FindBySeriesAndNumber(PersonUserControlViewModel.Series!, PersonUserControlViewModel.Number!);
            if (existing != null)
            {
                savedPerson = existing;
                savedPerson.LastName = PersonUserControlViewModel.LastName!;
                savedPerson.FirstName = PersonUserControlViewModel.FirstName!;
                savedPerson.Cnp = PersonUserControlViewModel.Cnp!;
                savedPerson.Email = PersonUserControlViewModel.Email;
                savedPerson.Phone = PersonUserControlViewModel.Phone;
                savedPerson.Series = PersonUserControlViewModel.Series!;
                savedPerson.Number = PersonUserControlViewModel.Number!;
                savedPerson.Issuer = PersonUserControlViewModel.Issuer!;
                if (PersonUserControlViewModel.IssuingDate.HasValue)
                    savedPerson.IssuingDate = DateOnly.FromDateTime(PersonUserControlViewModel.IssuingDate.Value.DateTime);
                savedPerson.Role = PersonUserControlViewModel.Role!;
                savedPerson.Address = personAddress;
                savedPerson.AddressId = personAddress.Id;
                await _personService.Update(savedPerson);
            }
            else
            {
                savedPerson = PersonUserControlViewModel.GetSelectedPerson();
                savedPerson.Address = personAddress;
                savedPerson.AddressId = personAddress.Id;
                await _personService.Add(savedPerson);
            }
        }
        else
        {
            savedPerson = PersonUserControlViewModel.SelectedPerson;
            savedPerson.LastName = PersonUserControlViewModel.LastName!;
            savedPerson.FirstName = PersonUserControlViewModel.FirstName!;
            savedPerson.Cnp = PersonUserControlViewModel.Cnp!;
            savedPerson.Email = PersonUserControlViewModel.Email;
            savedPerson.Phone = PersonUserControlViewModel.Phone;
            savedPerson.Series = PersonUserControlViewModel.Series!;
            savedPerson.Number = PersonUserControlViewModel.Number!;
            savedPerson.Issuer = PersonUserControlViewModel.Issuer!;
            if (PersonUserControlViewModel.IssuingDate.HasValue)
                savedPerson.IssuingDate = DateOnly.FromDateTime(PersonUserControlViewModel.IssuingDate.Value.DateTime);
            savedPerson.Role = PersonUserControlViewModel.Role!;
            savedPerson.Address = personAddress;
            savedPerson.AddressId = personAddress.Id;
            await _personService.Update(savedPerson);
        }
        await _personService.SubmitChanges();
        await PersonUserControlViewModel.LoadPersonsAsync();
        PersonUserControlViewModel.SelectedPerson = PersonUserControlViewModel.FilteredPersons.FirstOrDefault(p => p.Id == savedPerson.Id);

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
            savedClient.Address = address;
            savedClient.AddressId = address.Id;
            savedClient.Person = savedPerson;
            savedClient.PersonId = savedPerson.Id;
            await _clientService.Update(savedClient);
        }
        await _clientService.SubmitChanges();
        await LoadClientsAsync();
        SelectedClient = FilteredClients.FirstOrDefault(c => c.Id == savedClient.Id);
        
        return savedClient;
    }
}
