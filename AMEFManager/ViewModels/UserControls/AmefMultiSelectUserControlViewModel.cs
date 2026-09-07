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

public partial class AmefMultiSelectUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly AmefService _amefService;
    private readonly ClientService? _clientService;
    private List<Amef> _allAmefs = [];
    private List<Contract> _allContracts = [];

    public Func<IEnumerable<Amef>, List<string>>? Validator { get; set; }

    public AmefMultiSelectUserControlViewModel(AmefService amefService, ClientService? clientService = null)
    {
        _amefService = amefService;
        _clientService = clientService;
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

    [ObservableProperty] private ObservableCollection<Client> _availableClients = [];
    [ObservableProperty] private Client? _selectedClientFilter;
    [ObservableProperty] private string? _clientFilter;

    [ObservableProperty] private ObservableCollection<Contract> _availableContracts = [];
    [ObservableProperty] private Contract? _selectedContractFilter;

    [RelayCommand]
    private void ClearClientFilter()
    {
        ClientFilter = null;
        SelectedClientFilter = null;
    }

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
        
        _allContracts = _allAmefs
            .Select(a => a.Contract)
            .Where(c => c != null)
            .GroupBy(c => c!.Id)
            .Select(g => g.First()!)
            .OrderByDescending(c => c.Number)
            .ToList();

        if (_clientService != null)
        {
            var clients = await _clientService.FindAll();
            AvailableClients = new ObservableCollection<Client>(clients.OrderBy(c => c.Name));
        }
        else
        {
            var clients = _allAmefs
                .Select(a => a.Contract?.Client)
                .Where(c => c != null)
                .GroupBy(c => c!.Id)
                .Select(g => g.First()!)
                .OrderBy(c => c.Name)
                .ToList();
            AvailableClients = new ObservableCollection<Client>(clients);
        }

        UpdateAvailableContracts();
        ApplyFilter();
        IsLoading = false;
    }

    private void UpdateAvailableContracts()
    {
        var contracts = _allContracts;
        if (!string.IsNullOrWhiteSpace(ClientFilter))
        {
            contracts = contracts
                .Where(c => c.Client != null && (
                    c.Client.Name.Contains(ClientFilter, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(c.Client.NationalIdentifier) && c.Client.NationalIdentifier.Contains(ClientFilter, StringComparison.OrdinalIgnoreCase))
                ))
                .ToList();
        }
        else if (SelectedClientFilter != null)
        {
            contracts = contracts
                .Where(c => c.ClientId == SelectedClientFilter.Id || c.Client?.Id == SelectedClientFilter.Id)
                .ToList();
        }
        AvailableContracts = new ObservableCollection<Contract>(contracts);

        if (SelectedContractFilter != null && !AvailableContracts.Any(c => c.Id == SelectedContractFilter.Id))
        {
            SelectedContractFilter = null;
        }
    }

    partial void OnClientFilterChanged(string? value)
    {
        UpdateAvailableContracts();
        ApplyFilter();
    }

    partial void OnSelectedClientFilterChanged(Client? value)
    {
        UpdateAvailableContracts();
        ApplyFilter();
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
            (string.IsNullOrWhiteSpace(ClientFilter) || (a.Contract?.Client != null && (
                a.Contract.Client.Name.Contains(ClientFilter, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(a.Contract.Client.NationalIdentifier) && a.Contract.Client.NationalIdentifier.Contains(ClientFilter, StringComparison.OrdinalIgnoreCase))
            ))) &&
            (SelectedClientFilter == null || (a.Contract != null && (a.Contract.ClientId == SelectedClientFilter.Id || a.Contract.Client?.Id == SelectedClientFilter.Id))) &&
            (SelectedContractFilter == null || a.Contract?.Id == SelectedContractFilter.Id) &&
            !SelectedAmefs.Contains(a)
        ).OrderByDescending(x => x.Id).ToList();

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

    public void Dispose()
    {
    }
}
