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

public partial class SealingDocumentUserControlViewModel : ViewModelBase
{
    private readonly SealingDocumentService _sealingDocumentService;
    private List<SealingDocument> _allSealingDocuments = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public AmefUserControlViewModel AmefUserControlViewModel { get; }

    public SealingDocumentUserControlViewModel(
        SealingDocumentService sealingDocumentService,
        AmefService amefService,
        BillService billService,
        AddressService addressService,
        AuthorizationService authorizationService,
        ContractService contractService,
        ContractTypeService contractTypeService,
        ClientService clientService,
        PersonService personService)
    {
        _sealingDocumentService = sealingDocumentService;
        
        AmefUserControlViewModel = new AmefUserControlViewModel(
            amefService, billService, addressService, authorizationService, contractService, contractTypeService, clientService, personService);

        AmefUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(AmefUserControlViewModel.SelectedAmef) && !_isUpdatingFromSelection) ApplyFilter(); };
        AmefUserControlViewModel.ContractUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract) && !_isUpdatingFromSelection) ApplyFilter(); };
        AmefUserControlViewModel.ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };
        
        LoadSealingDocumentsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<SealingDocument> _filteredSealingDocuments = [];
    [ObservableProperty] private SealingDocument? _selectedSealingDocument;

    [ObservableProperty] private int? _number;
    [ObservableProperty] private DateTimeOffset? _date;

    [RelayCommand]
    private void ClearSelectedSealingDocument()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            // Disable the automatic OnSelectedSealingDocumentChanged logic
            _hasBeenFiltered = true;
            SelectedSealingDocument = null;

            // Manually clear all fields
            Number = null;
            Date = null;
            
            AmefUserControlViewModel.ClearSelectedAmefCommand.Execute(null);
        }
        finally
        {
            _isUpdatingFromSelection = false;
            ApplyFilter();
            _hasBeenFiltered = false;
        }
    }

    [RelayCommand]
    public async Task LoadSealingDocumentsAsync()
    {
        IsLoading = true;
        _allSealingDocuments = await _sealingDocumentService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedSealingDocumentChanged(SealingDocument? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        Debug.Print("Selected sealing document changed");
        
        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                Number = null;
                Date = null;
                
                AmefUserControlViewModel.ClearSelectedAmefCommand.Execute(null);
            }
            else
            {
                Number = value.Number;
                Date = new DateTimeOffset(value.Date.ToDateTime(TimeOnly.MinValue));

                AmefUserControlViewModel.SelectedAmef = AmefUserControlViewModel.FilteredAmefs.FirstOrDefault(a => a.Id == value.AmefId);
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    private void OnFieldChanged()
    {
        if (SelectedSealingDocument is not null) return;
        if (_isUpdatingFromSelection) return;
        ApplyFilter();
    }

    partial void OnNumberChanged(int? value) => OnFieldChanged();
    partial void OnDateChanged(DateTimeOffset? value) => OnFieldChanged();

    public void ApplyFilter()
    {
        _hasBeenFiltered = true;
        try
        {
                    var selectedAmef = AmefUserControlViewModel.SelectedAmef;
            var selectedContract = AmefUserControlViewModel.ContractUserControlViewModel.SelectedContract;
            var selectedClient = AmefUserControlViewModel.ContractUserControlViewModel.ClientUserControlViewModel.SelectedClient;

            var filtered = _allSealingDocuments.Where(s =>
                (Number == null || s.Number == Number) &&
                (selectedAmef == null || s.AmefId == selectedAmef.Id) &&
                (selectedContract == null || s.Amef?.ContractId == selectedContract.Id) &&
                (selectedClient == null || s.Amef?.Contract?.ClientId == selectedClient.Id) ||
                s.Equals(SelectedSealingDocument)
            ).OrderBy(x => x.Id).ToList();

            FilteredSealingDocuments = new ObservableCollection<SealingDocument>(filtered);
        }
        finally
        {
            _hasBeenFiltered = false;
        }
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Number == null || Number <= 0) errors.Add("Numarul documentului de sigilare este obligatoriu si trebuie sa fie mai mare decat 0.");
        if (Date is null) errors.Add("Data documentului de sigilare este obligatorie.");

        errors.AddRange(AmefUserControlViewModel.Validate());

        return errors;
    }

    public SealingDocument GetSelectedSealingDocument()
    {
        if (SelectedSealingDocument is null)
        {
            return new SealingDocument
            {
                Number = Number ?? 0,
                Date = DateOnly.FromDateTime(Date!.Value.DateTime)
            };
        }
        return SelectedSealingDocument;
    }
}
