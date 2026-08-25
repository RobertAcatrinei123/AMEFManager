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

public partial class AuthorizationUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly AuthorizationService _authorizationService;
    private List<Authorization> _allAuthorizations = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public AuthorizationUserControlViewModel(AuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
        LoadAuthorizationsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Autorizație Distribuitor";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Authorization> _filteredAuthorizations = [];

    [ObservableProperty]
    private Authorization? _selectedAuthorization;

    [ObservableProperty]
    private int? _number;

    [ObservableProperty]
    private DateTimeOffset? _date;

    [ObservableProperty]
    private string? _model;

    [ObservableProperty]
    private string? _brand;

    [ObservableProperty]
    private string? _deviceType;

    [ObservableProperty]
    private string? _configuration;

    [RelayCommand]
    private void ClearSelectedAuthorization()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            _hasBeenFiltered = true;
            SelectedAuthorization = null;

            Number = null;
            Date = null;
            Model = null;
            Brand = null;
            DeviceType = null;
            Configuration = null;
        }
        finally
        {
            _isUpdatingFromSelection = false;
            ApplyFilter();
            _hasBeenFiltered = false;
        }
    }

    [RelayCommand]
    public async Task LoadAuthorizationsAsync()
    {
        IsLoading = true;
        _allAuthorizations = await _authorizationService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedAuthorizationChanged(Authorization? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected Authorization changed: Id={value?.Id}, Number={value?.Number}, Model={value?.Model}");

        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                Number = null;
                Date = null;
                Model = null;
                Brand = null;
                DeviceType = null;
                Configuration = null;
            }
            else
            {
                Number = value.Number;
                Date = new DateTimeOffset(value.Date.ToDateTime(TimeOnly.MinValue));
                Model = value.Model;
                Brand = value.Brand;
                DeviceType = value.DeviceType;
                Configuration = value.Configuration;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnModelChanged(string? value) => OnFieldChanged();
    partial void OnBrandChanged(string? value) => OnFieldChanged();
    partial void OnDeviceTypeChanged(string? value) => OnFieldChanged();
    partial void OnConfigurationChanged(string? value) => OnFieldChanged();

    partial void OnNumberChanged(int? value) => OnFieldChanged();
    partial void OnDateChanged(DateTimeOffset? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedAuthorization is not null)
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
            var filtered = _allAuthorizations.Where(a =>
                (Number == null || a.Number == Number) &&
                StartsWith(a.Model, Model) &&
                StartsWith(a.Brand, Brand) &&
                StartsWith(a.DeviceType, DeviceType) &&
                StartsWith(a.Configuration, Configuration) ||
                a.Equals(SelectedAuthorization)
            ).OrderBy(x => x.Number).ToList();

            FilteredAuthorizations = new ObservableCollection<Authorization>(filtered);
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

        if (Number == null || Number <= 0)
            errors.Add("Numarul autorizatiei este obligatoriu.");
        if (Date is null)
            errors.Add("Data autorizatiei este obligatorie.");
        if (string.IsNullOrWhiteSpace(Model))
            errors.Add("Modelul este obligatoriu.");
        if (string.IsNullOrWhiteSpace(Brand))
            errors.Add("Brandul este obligatoriu.");
        if (string.IsNullOrWhiteSpace(DeviceType))
            errors.Add("Tipul aparatului este obligatoriu.");
        if (string.IsNullOrWhiteSpace(Configuration))
            errors.Add("Configuratia este obligatorie.");

        return errors;
    }

    public Authorization GetSelectedAuthorization()
    {
        if (SelectedAuthorization is null)
        {
            return new Authorization
            {
                Number = Number ?? 0,
                Date = DateOnly.FromDateTime(Date!.Value.DateTime),
                Model = Model ?? string.Empty,
                Brand = Brand ?? string.Empty,
                DeviceType = DeviceType ?? string.Empty,
                Configuration = Configuration ?? string.Empty
            };
        }

        return SelectedAuthorization;
    }

    public async Task<Authorization> SaveAuthorizationAsync()
    {
        Authorization savedAuthorization;

        if (SelectedAuthorization is null)
        {
            savedAuthorization = GetSelectedAuthorization();
            await _authorizationService.Add(savedAuthorization);
        }
        else
        {
            savedAuthorization = SelectedAuthorization;
            savedAuthorization.Number = Number ?? 0;
            if (Date.HasValue)
                savedAuthorization.Date = DateOnly.FromDateTime(Date.Value.DateTime);
            savedAuthorization.Model = Model!;
            savedAuthorization.Brand = Brand!;
            savedAuthorization.DeviceType = DeviceType!;
            savedAuthorization.Configuration = Configuration!;
            await _authorizationService.Update(savedAuthorization);
        }

        await _authorizationService.SubmitChanges();
        await LoadAuthorizationsAsync();
        SelectedAuthorization = FilteredAuthorizations.FirstOrDefault(a => a.Id == savedAuthorization.Id);
        AppLogger.LogInfo($"Successfully saved Authorization: Id={savedAuthorization.Id}, Number={savedAuthorization.Number}, Model={savedAuthorization.Model}");

        return savedAuthorization;
    }

    public async Task DeleteAuthorizationAsync()
    {
        if (SelectedAuthorization is null)
            return;

        var toDelete = SelectedAuthorization;
        AppLogger.LogInfo($"Deleting Authorization: Id={toDelete.Id}, Number={toDelete.Number}, Model={toDelete.Model}");

        await _authorizationService.Delete(toDelete);
        await _authorizationService.SubmitChanges();

        ClearSelectedAuthorization();
        await LoadAuthorizationsAsync();
        AppLogger.LogInfo($"Authorization Id={toDelete.Id} deleted successfully.");
    }

    public void Dispose()
    {
    }
}
