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

namespace AMEFManager.ViewModels.UserControls;

public partial class AddressUserControlViewModel : ViewModelBase
{
    private readonly AddressService _addressService;
    private List<Address> _allAddresses = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public AddressUserControlViewModel(AddressService addressService)
    {
        _addressService = addressService;
        LoadAddressesCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Adresă";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Address> _filteredAddresses = [];

    [ObservableProperty]
    private Address? _selectedAddress;

    [ObservableProperty]
    private string? _county;

    [ObservableProperty]
    private string? _city;

    [ObservableProperty]
    private string? _street;

    [ObservableProperty]
    private string? _streetNumber;

    [ObservableProperty]
    private string? _block;

    [ObservableProperty]
    private string? _floor;

    [ObservableProperty]
    private string? _apartment;

    [ObservableProperty]
    private string? _other;

    [RelayCommand]
    private void ClearSelectedAddress()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            _hasBeenFiltered = true;
            SelectedAddress = null;

            County = null;
            City = null;
            Street = null;
            StreetNumber = null;
            Block = null;
            Floor = null;
            Apartment = null;
            Other = null;
        }
        finally
        {
            _isUpdatingFromSelection = false;
            ApplyFilter();
            _hasBeenFiltered = false;
        }
    }

    [RelayCommand]
    public async Task LoadAddressesAsync()
    {
        IsLoading = true;
        _allAddresses = await _addressService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedAddressChanged(Address? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }
        
        AppLogger.LogDebug($"Selected Address changed: Id={value?.Id}, City={value?.City}, Street={value?.Street}");
        
        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                County = null;
                City = null;
                Street = null;
                StreetNumber = null;
                Block = null;
                Floor = null;
                Apartment = null;
                Other = null;
            }
            else
            {
                County = value.County;
                City = value.City;
                Street = value.Street;
                StreetNumber = value.StreetNumber;
                Block = value.Block;
                Floor = value.Floor;
                Apartment = value.Apartment;
                Other = value.Other;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnCountyChanged(string? value) => OnFieldChanged();
    partial void OnCityChanged(string? value) => OnFieldChanged();
    partial void OnStreetChanged(string? value) => OnFieldChanged();
    partial void OnStreetNumberChanged(string? value) => OnFieldChanged();
    partial void OnBlockChanged(string? value) => OnFieldChanged();
    partial void OnFloorChanged(string? value) => OnFieldChanged();
    partial void OnApartmentChanged(string? value) => OnFieldChanged();
    partial void OnOtherChanged(string? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedAddress is not null)
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
                
            var filtered = _allAddresses.Where(a =>
                StartsWith(a.County, County) &&
                StartsWith(a.City, City) &&
                StartsWith(a.Street, Street) &&
                StartsWith(a.StreetNumber, StreetNumber) &&
                StartsWith(a.Block, Block) &&
                StartsWith(a.Floor, Floor) &&
                StartsWith(a.Apartment, Apartment) &&
                StartsWith(a.Other, Other) ||
                a.Equals(SelectedAddress)
            ).OrderBy(x => x.Id).ToList();

            FilteredAddresses = new ObservableCollection<Address>(filtered);
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

        bool isEmpty = string.IsNullOrWhiteSpace(County) &&
                       string.IsNullOrWhiteSpace(City) &&
                       string.IsNullOrWhiteSpace(Street) &&
                       string.IsNullOrWhiteSpace(StreetNumber) &&
                       string.IsNullOrWhiteSpace(Block) &&
                       string.IsNullOrWhiteSpace(Floor) &&
                       string.IsNullOrWhiteSpace(Apartment) &&
                       string.IsNullOrWhiteSpace(Other);

        if (isEmpty)
        {
            errors.Add("Adresa nu poate fi complet goală. Vă rugăm să completați cel puțin un câmp.");
        }

        return errors;
    }

    public Address GetSelectedAddress()
    {
        if (SelectedAddress is null)
        {
            return new Address
            {
                County = County,
                City = City,
                Street = Street,
                StreetNumber = StreetNumber,
                Block = Block,
                Floor = Floor,
                Apartment = Apartment,
                Other = Other
            };
        }

        return SelectedAddress;
    }
    
    public async Task<Address> SaveAddressAsync()
    {
        Address savedAddress;

        if (SelectedAddress is null)
        {
            savedAddress = GetSelectedAddress();
            await _addressService.Add(savedAddress);
        }
        else
        {
            savedAddress = SelectedAddress;
            savedAddress.County = County;
            savedAddress.City = City;
            savedAddress.Street = Street;
            savedAddress.StreetNumber = StreetNumber;
            savedAddress.Block = Block;
            savedAddress.Floor = Floor;
            savedAddress.Apartment = Apartment;
            savedAddress.Other = Other;
            await _addressService.Update(savedAddress);
        }

        await _addressService.SubmitChanges();
        await LoadAddressesAsync();
        SelectedAddress = FilteredAddresses.FirstOrDefault(a => a.Id == savedAddress.Id);

        return savedAddress;
    }

    public async Task DeleteAddressAsync()
    {
        if (SelectedAddress is null)
            return;

        var toDelete = SelectedAddress;
        AppLogger.LogInfo($"Deleting Address: Id={toDelete.Id}, City={toDelete.City}, Street={toDelete.Street}");

        await _addressService.Delete(toDelete);
        await _addressService.SubmitChanges();

        ClearSelectedAddress();
        await LoadAddressesAsync();
        AppLogger.LogInfo($"Address Id={toDelete.Id} deleted successfully.");
    }
}
