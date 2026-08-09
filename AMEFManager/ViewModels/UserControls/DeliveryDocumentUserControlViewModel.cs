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

public partial class DeliveryDocumentUserControlViewModel : ViewModelBase
{
    private readonly DeliveryDocumentService _deliveryDocumentService;
    private List<DeliveryDocument> _allDeliveryDocuments = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public AmefUserControlViewModel AmefUserControlViewModel { get; }

    public DeliveryDocumentUserControlViewModel(
        DeliveryDocumentService deliveryDocumentService,
        AmefService amefService,
        BillService billService,
        AddressService addressService,
        AuthorizationService authorizationService,
        ContractService contractService,
        ContractTypeService contractTypeService,
        ClientService clientService,
        PersonService personService)
    {
        _deliveryDocumentService = deliveryDocumentService;

        AmefUserControlViewModel = new AmefUserControlViewModel(
            amefService, billService, addressService, authorizationService, contractService, contractTypeService, clientService, personService);

        AmefUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(AmefUserControlViewModel.SelectedAmef) && !_isUpdatingFromSelection) ApplyFilter(); };
        AmefUserControlViewModel.ContractUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract) && !_isUpdatingFromSelection) ApplyFilter(); };
        AmefUserControlViewModel.ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };

        LoadDeliveryDocumentsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<DeliveryDocument> _filteredDeliveryDocuments = [];
    [ObservableProperty] private DeliveryDocument? _selectedDeliveryDocument;

    [ObservableProperty] private int? _number;
    [ObservableProperty] private DateTimeOffset? _date;

        [RelayCommand]
    private void ClearSelectedDeliveryDocument()
    {
        _isUpdatingFromSelection = true;
        
        try
        {
            // Disable the automatic OnSelectedDeliveryDocumentChanged logic
            _hasBeenFiltered = true;
            SelectedDeliveryDocument = null;

            // Manually clear all fields
            Number = null;
            Date = null;
            
            AmefUserControlViewModel.ClearSelectedAmefCommand.Execute(null);
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }

        ApplyFilter();
        _hasBeenFiltered = false;
    }

    [RelayCommand]
    public async Task LoadDeliveryDocumentsAsync()
    {
        IsLoading = true;
        _allDeliveryDocuments = await _deliveryDocumentService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedDeliveryDocumentChanged(DeliveryDocument? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        Debug.Print("Selected delivery document changed");
        _isUpdatingFromSelection = true;

        try
        {
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
        if (SelectedDeliveryDocument is not null) return;
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

            var filtered = _allDeliveryDocuments.Where(d =>
                (Number == null || d.Number == Number) &&
                (selectedAmef == null || d.AmefId == selectedAmef.Id) &&
                (selectedContract == null || d.Amef?.ContractId == selectedContract.Id) &&
                (selectedClient == null || d.Amef?.Contract?.ClientId == selectedClient.Id) ||
                d.Equals(SelectedDeliveryDocument)
            ).OrderBy(x => x.Id).ToList();

            FilteredDeliveryDocuments = new ObservableCollection<DeliveryDocument>(filtered);
        }
        finally
        {
            _hasBeenFiltered = false;
        }
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Number == null || Number <= 0) errors.Add("Numarul procesului verbal trebuie sa fie mai mare ca 0.");
        if (Date is null) errors.Add("Data procesului verbal este obligatorie.");
        
        errors.AddRange(AmefUserControlViewModel.Validate());

        return errors;
    }

    public DeliveryDocument GetSelectedDeliveryDocument()
    {
        if (SelectedDeliveryDocument is null)
        {
            return new DeliveryDocument
            {
                Number = Number ?? 0,
                Date = DateOnly.FromDateTime(Date!.Value.DateTime)
            };
        }
        return SelectedDeliveryDocument;
    }
}
