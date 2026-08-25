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

public partial class DeliveryDocumentUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly DeliveryDocumentService _deliveryDocumentService;
    private readonly PropertyChangedEventHandler _amefHandler;
    private readonly PropertyChangedEventHandler _contractHandler;
    private readonly PropertyChangedEventHandler _clientHandler;
    private List<DeliveryDocument> _allDeliveryDocuments = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;
    private bool _disposed;

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
            amefService, billService, addressService, authorizationService, contractService, contractTypeService, clientService, personService)
        {
            HeaderTitle = "AMEF Predat"
        };

        _amefHandler = (s, e) => { if (e.PropertyName == nameof(AmefUserControlViewModel.SelectedAmef) && !_isUpdatingFromSelection) ApplyFilter(); };
        _contractHandler = (s, e) => { if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract) && !_isUpdatingFromSelection) ApplyFilter(); };
        _clientHandler = (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };

        AmefUserControlViewModel.PropertyChanged += _amefHandler;
        AmefUserControlViewModel.ContractUserControlViewModel.PropertyChanged += _contractHandler;
        AmefUserControlViewModel.ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged += _clientHandler;

        LoadDeliveryDocumentsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty] private string _headerTitle = "Date Proces Verbal de Predare-Primire";
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
            _hasBeenFiltered = true;
            SelectedDeliveryDocument = null;

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

        AppLogger.LogDebug($"Selected DeliveryDocument changed: Id={value?.Id}, Number={value?.Number}");
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
            ).OrderBy(x => x.Number).ToList();

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

    public async Task<DeliveryDocument> SaveDeliveryDocumentAsync()
    {
        var savedAmef = await AmefUserControlViewModel.SaveAmefAsync();

        DeliveryDocument savedDeliveryDocument;
        if (SelectedDeliveryDocument is null)
        {
            var existing = await _deliveryDocumentService.FindByNumber(Number ?? 0);
            if (existing != null)
            {
                savedDeliveryDocument = existing;
                savedDeliveryDocument.Date = DateOnly.FromDateTime(Date!.Value.DateTime);
                savedDeliveryDocument.Amef = savedAmef;
                savedDeliveryDocument.AmefId = savedAmef.Id;
                await _deliveryDocumentService.Update(savedDeliveryDocument);
            }
            else
            {
                savedDeliveryDocument = GetSelectedDeliveryDocument();
                savedDeliveryDocument.Amef = savedAmef;
                savedDeliveryDocument.AmefId = savedAmef.Id;
                await _deliveryDocumentService.Add(savedDeliveryDocument);
            }
        }
        else
        {
            savedDeliveryDocument = SelectedDeliveryDocument;
            savedDeliveryDocument.Number = Number ?? 0;
            savedDeliveryDocument.Date = DateOnly.FromDateTime(Date!.Value.DateTime);
            savedDeliveryDocument.Amef = savedAmef;
            savedDeliveryDocument.AmefId = savedAmef.Id;
            await _deliveryDocumentService.Update(savedDeliveryDocument);
        }

        await _deliveryDocumentService.SubmitChanges();
        await LoadDeliveryDocumentsAsync();
        SelectedDeliveryDocument = FilteredDeliveryDocuments.FirstOrDefault(d => d.Id == savedDeliveryDocument.Id);
        AppLogger.LogInfo($"Successfully saved DeliveryDocument: Id={savedDeliveryDocument.Id}, Number={savedDeliveryDocument.Number}, Date={savedDeliveryDocument.Date}");

        return savedDeliveryDocument;
    }

    public async Task DeleteDeliveryDocumentAsync()
    {
        if (SelectedDeliveryDocument is null) return;

        var toDelete = SelectedDeliveryDocument;
        AppLogger.LogInfo($"Deleting DeliveryDocument: Id={toDelete.Id}, Number={toDelete.Number}");

        await _deliveryDocumentService.Delete(toDelete);
        await _deliveryDocumentService.SubmitChanges();
        ClearSelectedDeliveryDocument();
        await LoadDeliveryDocumentsAsync();
        AppLogger.LogInfo($"DeliveryDocument Id={toDelete.Id} deleted successfully.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AmefUserControlViewModel.PropertyChanged -= _amefHandler;
        AmefUserControlViewModel.ContractUserControlViewModel.PropertyChanged -= _contractHandler;
        AmefUserControlViewModel.ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged -= _clientHandler;

        AmefUserControlViewModel.Dispose();
    }
}
