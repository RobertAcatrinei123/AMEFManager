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

public partial class PersonUserControlViewModel : ViewModelBase
{
    private readonly PersonService _personService;
    private List<Person> _allPersons = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public AddressUserControlViewModel AddressUserControlViewModel { get; }

    public PersonUserControlViewModel(PersonService personService, AddressService addressService)
    {
        _personService = personService;
        AddressUserControlViewModel = new AddressUserControlViewModel(addressService);
        LoadPersonsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

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

        Debug.Print("Selected person changed");

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
                IssuingDate = new DateTimeOffset(value.IssuingDate.ToDateTime(TimeOnly.MinValue));
                Role = value.Role;

                var matchingAddress = AddressUserControlViewModel.FilteredAddresses
                    .FirstOrDefault(a => a.Id == value.AddressId);
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
                    var filtered = _allPersons.Where(p =>
                StartsWith(p.LastName, LastName) &&
                StartsWith(p.FirstName, FirstName) &&
                StartsWith(p.Cnp, Cnp) &&
                StartsWith(p.Email, Email) &&
                StartsWith(p.Phone, Phone) &&
                StartsWith(p.Series, Series) &&
                StartsWith(p.Number, Number) &&
                StartsWith(p.Issuer, Issuer) &&
                StartsWith(p.Role, Role) ||
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
}
