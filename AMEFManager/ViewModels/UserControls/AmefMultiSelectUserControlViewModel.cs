using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.UserControls;

public partial class AmefMultiSelectUserControlViewModel : ViewModelBase
{
    private readonly AmefService _amefService;
    private List<Amef> _allAmefs = [];

    public Func<IEnumerable<Amef>, List<string>>? Validator { get; set; }

    public AmefMultiSelectUserControlViewModel(AmefService amefService)
    {
        _amefService = amefService;
        SelectedAmefs = new ObservableCollection<Amef>();
        LoadAmefsCommand.Execute(null);
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<Amef> _filteredAmefs = [];
    [ObservableProperty] private ObservableCollection<Amef> _selectedAmefs;
    
    [ObservableProperty] private Amef? _amefToAdd;

    [ObservableProperty] private string? _model;
    [ObservableProperty] private string? _series;
    [ObservableProperty] private string? _nui;

    [ObservableProperty] private ObservableCollection<Contract> _availableContracts = [];
    [ObservableProperty] private Contract? _selectedContractFilter;

    [RelayCommand]
    private void ClearContractFilter()
    {
        SelectedContractFilter = null;
    }

    [RelayCommand]
    public async Task LoadAmefsAsync()
    {
        IsLoading = true;
        _allAmefs = await _amefService.FindAll();
        
        var contracts = _allAmefs
            .Select(a => a.Contract)
            .GroupBy(c => c!.Id)
            .Select(g => g.First())
            .OrderBy(c => c!.Number)
            .ToList();
            
        AvailableContracts = new ObservableCollection<Contract>(contracts!);
        
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnAmefToAddChanged(Amef? value)
    {
        if (value is not null)
        {
            if (!SelectedAmefs.Contains(value))
            {
                SelectedAmefs.Add(value);
            }
            
            AmefToAdd = null;
            
            Model = null;
            Series = null;
            Nui = null;
            
            ApplyFilter();
        }
    }

    [RelayCommand]
    private void RemoveAmef(Amef amef)
    {
        if (amef is not null && SelectedAmefs.Contains(amef))
        {
            SelectedAmefs.Remove(amef);
            ApplyFilter();
        }
    }

    partial void OnModelChanged(string? value) => ApplyFilter();
    partial void OnSeriesChanged(string? value) => ApplyFilter();
    partial void OnNuiChanged(string? value) => ApplyFilter();
    partial void OnSelectedContractFilterChanged(Contract? value) => ApplyFilter();

    public void ApplyFilter()
    {
        var filtered = _allAmefs.Where(a =>
            StartsWith(a.Model, Model) &&
            StartsWith(a.Series, Series) &&
            StartsWith(a.NUI, Nui) &&
            (SelectedContractFilter == null || a.Contract?.Id == SelectedContractFilter.Id) &&
            !SelectedAmefs.Contains(a)
        ).OrderBy(x => x.Id).ToList();

        FilteredAmefs = new ObservableCollection<Amef>(filtered);
    }

    private static bool StartsWith(string? value, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    public List<string> Validate()
    {
        if (Validator != null)
        {
            return Validator(SelectedAmefs);
        }
        return [];
    }
}
