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

public partial class AuthorizationUserControlViewModel : ViewModelBase
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

    [RelayCommand]
    private void ClearSelectedAuthorization()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            // Disable the automatic OnSelectedAuthorizationChanged logic
            _hasBeenFiltered = true;
            SelectedAuthorization = null;

            // Manually clear all fields
            Number = null;
            Date = null;
            Model = null;
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

        Debug.Print("Selected authorization changed");

        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                Number = null;
                Date = null;
                Model = null;
            }
            else
            {
                Number = value.Number;
                Date = new DateTimeOffset(value.Date.ToDateTime(TimeOnly.MinValue));
                Model = value.Model;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnModelChanged(string? value) => OnFieldChanged();

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
                StartsWith(a.Model, Model) ||
                a.Equals(SelectedAuthorization)
            ).OrderBy(x => x.Id).ToList();

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
                Model = Model!
            };
        }

        return SelectedAuthorization;
    }
}
