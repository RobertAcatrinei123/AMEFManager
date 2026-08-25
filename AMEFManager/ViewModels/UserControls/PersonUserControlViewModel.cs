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

public partial class PersonUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly PersonService _personService;
    private readonly AddressService _addressService;
    private readonly PropertyChangedEventHandler _addressHandler;
    private List<Person> _allPersons = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;
    private bool _disposed;

    public AddressUserControlViewModel AddressUserControlViewModel { get; }

    public PersonUserControlViewModel(PersonService personService, AddressService addressService, List<Address>? initialAddresses = null)
    {
        _personService = personService;
        _addressService = addressService;
        AddressUserControlViewModel = new AddressUserControlViewModel(addressService, initialAddresses)
        {
            HeaderTitle = "Adresă Domiciliu Persoană"
        };
        _addressHandler = (s, e) => { if (e.PropertyName == nameof(AddressUserControlViewModel.SelectedAddress) && !_isUpdatingFromSelection) ApplyFilter(); };
        AddressUserControlViewModel.PropertyChanged += _addressHandler;

        LoadPersonsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Personale & Contact";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Person> _filteredPersons = [];

    [ObservableProperty]
    private Person? _selectedPerson;

    [ObservableProperty]
    private string? _lastName;

    [ObservableProperty]
    private string? _firstName;

    [ObservableProperty]
    private string? _cnp;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _series;

    [ObservableProperty]
    private string? _number;

    [ObservableProperty]
    private string? _issuer;

    [ObservableProperty]
    private DateTimeOffset? _issuingDate;

    [ObservableProperty]
    private string? _role;

    [RelayCommand]
    private void ClearSelectedPerson()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            _hasBeenFiltered = true;
            SelectedPerson = null;

            LastName = null;
            FirstName = null;
            Cnp = null;
            Email = null;
            Phone = null;
            Series = null;
            Number = null;
            Issuer = null;
            IssuingDate = null;
            Role = null;
            AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
        }
        finally
        {
            _isUpdatingFromSelection = false;
            ApplyFilter();
            _hasBeenFiltered = false;
        }
    }

    [RelayCommand]
    public async Task LoadPersonsAsync()
    {
        IsLoading = true;
        _allPersons = await _personService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedPersonChanged(Person? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected Person changed: Id={value?.Id}, Name={value?.FirstName} {value?.LastName}");

        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                LastName = null;
                FirstName = null;
                Cnp = null;
                Email = null;
                Phone = null;
                Series = null;
                Number = null;
                Issuer = null;
                IssuingDate = null;
                Role = null;
                AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
            }
            else
            {
                LastName = value.LastName;
                FirstName = value.FirstName;
                Cnp = value.Cnp;
                Email = value.Email;
                Phone = value.Phone;
                Series = value.Series;
                Number = value.Number;
                Issuer = value.Issuer;
                IssuingDate = DateHelper.ToDateTimeOffset(value.IssuingDate);
                Role = value.Role;

                var matchingAddress = AddressUserControlViewModel.FilteredAddresses
                    .FirstOrDefault(a => a.Id == value.AddressId) ?? value.Address;
                AddressUserControlViewModel.SelectedAddress = matchingAddress;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnLastNameChanged(string? value) => OnFieldChanged();
    partial void OnFirstNameChanged(string? value) => OnFieldChanged();
    partial void OnCnpChanged(string? value) => OnFieldChanged();
    partial void OnEmailChanged(string? value) => OnFieldChanged();
    partial void OnPhoneChanged(string? value) => OnFieldChanged();
    partial void OnSeriesChanged(string? value) => OnFieldChanged();
    partial void OnNumberChanged(string? value) => OnFieldChanged();
    partial void OnIssuerChanged(string? value) => OnFieldChanged();
    partial void OnRoleChanged(string? value) => OnFieldChanged();

    partial void OnIssuingDateChanged(DateTimeOffset? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedPerson is not null)
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

            var filtered = _allPersons.Where(p =>
                StartsWith(p.LastName, LastName) &&
                StartsWith(p.FirstName, FirstName) &&
                StartsWith(p.Cnp, Cnp) &&
                StartsWith(p.Email, Email) &&
                StartsWith(p.Phone, Phone) &&
                StartsWith(p.Series, Series) &&
                StartsWith(p.Number, Number) &&
                StartsWith(p.Issuer, Issuer) &&
                StartsWith(p.Role, Role) &&
                (selectedAddress == null || p.AddressId == selectedAddress.Id) ||
                p.Equals(SelectedPerson)
            ).OrderBy(x => x.Id).ToList();

            FilteredPersons = new ObservableCollection<Person>(filtered);
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

        if (string.IsNullOrWhiteSpace(LastName))
            errors.Add("Numele este obligatoriu.");
        if (string.IsNullOrWhiteSpace(FirstName))
            errors.Add("Prenumele este obligatoriu.");
        if (string.IsNullOrWhiteSpace(Cnp))
            errors.Add("CNP-ul este obligatoriu.");
        if (string.IsNullOrWhiteSpace(Series))
            errors.Add("Seria CI este obligatorie.");
        if (string.IsNullOrWhiteSpace(Number))
            errors.Add("Numarul CI este obligatoriu.");
        if (string.IsNullOrWhiteSpace(Issuer))
            errors.Add("Emitentul CI este obligatoriu.");
        if (IssuingDate is null)
            errors.Add("Data emiterii CI este obligatorie.");
        if (string.IsNullOrWhiteSpace(Role))
            errors.Add("Rolul este obligatoriu.");

        if (AddressUserControlViewModel.SelectedAddress is null)
            errors.AddRange(AddressUserControlViewModel.Validate());

        return errors;
    }

    public Person GetSelectedPerson()
    {
        if (SelectedPerson is null)
        {
            return new Person
            {
                LastName = LastName!,
                FirstName = FirstName!,
                Cnp = Cnp!,
                Email = Email,
                Phone = Phone,
                Series = Series!,
                Number = Number!,
                Issuer = Issuer!,
                IssuingDate = DateOnly.FromDateTime(IssuingDate!.Value.DateTime),
                Role = Role!
            };
        }

        return SelectedPerson;
    }

    public async Task<Person> SavePersonAsync()
    {
        var addressVm = AddressUserControlViewModel;
        var address = await addressVm.SaveAddressAsync();

        Person savedPerson;

        if (SelectedPerson is null)
        {
            var existing = await _personService.FindByCnp(Cnp!) ?? 
                           await _personService.FindBySeriesAndNumber(Series!, Number!);
            
            if (existing != null)
            {
                savedPerson = existing;
                savedPerson.LastName = LastName!;
                savedPerson.FirstName = FirstName!;
                savedPerson.Cnp = Cnp!;
                savedPerson.Email = Email;
                savedPerson.Phone = Phone;
                savedPerson.Series = Series!;
                savedPerson.Number = Number!;
                savedPerson.Issuer = Issuer!;
                if (IssuingDate.HasValue)
                    savedPerson.IssuingDate = DateOnly.FromDateTime(IssuingDate.Value.DateTime);
                savedPerson.Role = Role!;
                savedPerson.Address = address;
                savedPerson.AddressId = address.Id;
                await _personService.Update(savedPerson);
            }
            else
            {
                savedPerson = GetSelectedPerson();
                savedPerson.Address = address;
                savedPerson.AddressId = address.Id;
                await _personService.Add(savedPerson);
            }
        }
        else
        {
            savedPerson = SelectedPerson;
            savedPerson.LastName = LastName!;
            savedPerson.FirstName = FirstName!;
            savedPerson.Cnp = Cnp!;
            savedPerson.Email = Email;
            savedPerson.Phone = Phone;
            savedPerson.Series = Series!;
            savedPerson.Number = Number!;
            savedPerson.Issuer = Issuer!;
            if (IssuingDate.HasValue)
                savedPerson.IssuingDate = DateOnly.FromDateTime(IssuingDate.Value.DateTime);
            savedPerson.Role = Role!;
            savedPerson.Address = address;
            savedPerson.AddressId = address.Id;
            await _personService.Update(savedPerson);
        }

        await _personService.SubmitChanges();
        await LoadPersonsAsync();

        SelectedPerson = FilteredPersons.FirstOrDefault(p => p.Id == savedPerson.Id);
        AppLogger.LogInfo($"Successfully saved Person: Id={savedPerson.Id}, Name={savedPerson.FirstName} {savedPerson.LastName}, CNP={savedPerson.Cnp}");
        return savedPerson;
    }

    public async Task DeletePersonAsync()
    {
        if (SelectedPerson is null) return;

        var toDelete = SelectedPerson;
        AppLogger.LogInfo($"Deleting Person: Id={toDelete.Id}, Name={toDelete.FirstName} {toDelete.LastName}");

        var address = toDelete.Address ?? (toDelete.AddressId > 0 ? await _addressService.FindById(toDelete.AddressId) : null);

        await _personService.Delete(toDelete);

        try
        {
            if (address != null)
            {
                await _addressService.Delete(address);
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Cascading delete address for Person Id={toDelete.Id} threw: {ex.Message}");
        }

        await _personService.SubmitChanges();
        ClearSelectedPerson();
        await LoadPersonsAsync();
        await AddressUserControlViewModel.LoadAddressesAsync();
        AppLogger.LogInfo($"Person Id={toDelete.Id} deleted successfully.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AddressUserControlViewModel.PropertyChanged -= _addressHandler;
        AddressUserControlViewModel.Dispose();
    }
}
