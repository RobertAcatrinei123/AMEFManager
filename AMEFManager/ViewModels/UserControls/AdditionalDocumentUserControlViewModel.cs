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

public partial class AdditionalDocumentUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly AdditionalDocumentService _documentService;
    private readonly PropertyChangedEventHandler _contractHandler;
    private readonly PropertyChangedEventHandler _clientHandler;
    private List<AdditionalDocument> _allDocuments = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;
    private bool _disposed;

    public ContractUserControlViewModel ContractUserControlViewModel { get; }

    public AdditionalDocumentUserControlViewModel(AdditionalDocumentService documentService, ContractService contractService, ContractTypeService contractTypeService, ClientService clientService, AddressService addressService, PersonService personService)
    {
        _documentService = documentService;
        ContractUserControlViewModel = new ContractUserControlViewModel(contractService, contractTypeService, clientService, addressService, personService)
        {
            HeaderTitle = "Contract de Bază"
        };
        
        _contractHandler = (s, e) => { if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract) && !_isUpdatingFromSelection) ApplyFilter(); };
        _clientHandler = (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };
        ContractUserControlViewModel.PropertyChanged += _contractHandler;
        ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged += _clientHandler;

        LoadDocumentsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Act Adițional";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<AdditionalDocument> _filteredDocuments = [];

    [ObservableProperty]
    private AdditionalDocument? _selectedDocument;

    [ObservableProperty]

    private int? _number;
    [ObservableProperty]
    private DateTimeOffset? _date;

        [RelayCommand]
    private void ClearSelectedDocument()
    {
        _isUpdatingFromSelection = true;
        
        try
        {
            _hasBeenFiltered = true;
            SelectedDocument = null;

            Number = null;
            Date = null;
            ContractUserControlViewModel.ClearSelectedContractCommand.Execute(null);
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }

        ApplyFilter();
        _hasBeenFiltered = false;
    }
    [RelayCommand]
    public async Task LoadDocumentsAsync()
    {
        IsLoading = true;
        _allDocuments = await _documentService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedDocumentChanged(AdditionalDocument? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected AdditionalDocument changed: Id={value?.Id}, Number={value?.Number}");

        _isUpdatingFromSelection = true;

        try
        {
            if (value is null)
            {
                Number = null;
                Date = null;
                ContractUserControlViewModel.ClearSelectedContractCommand.Execute(null);
            }
            else
            {
                Number = value.Number;
                Date = DateHelper.ToDateTimeOffset(value.Date);

                var matchingContract = ContractUserControlViewModel.FilteredContracts
                    .FirstOrDefault(c => c.Id == value.ContractId);
                ContractUserControlViewModel.SelectedContract = matchingContract;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnNumberChanged(int? value) => OnFieldChanged();
    partial void OnDateChanged(DateTimeOffset? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedDocument is not null)
            return;

        if (_isUpdatingFromSelection)
            return;

        ApplyFilter();
    }

    public void ApplyFilter()
    {
        _hasBeenFiltered = true;
        try
        {
            var selectedContract = ContractUserControlViewModel.SelectedContract;
            var selectedClient = ContractUserControlViewModel.ClientUserControlViewModel.SelectedClient;

            var filtered = _allDocuments.Where(d =>
                (Number == null || d.Number == Number) &&
                (selectedContract == null || d.ContractId == selectedContract.Id) &&
                (selectedClient == null || d.Contract?.ClientId == selectedClient.Id) ||
                d.Equals(SelectedDocument)
            ).OrderBy(x => x.Number).ToList();

            FilteredDocuments = new ObservableCollection<AdditionalDocument>(filtered);
        }
        finally
        {
            _hasBeenFiltered = false;
        }
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Number == null || Number <= 0)
            errors.Add("Numarul documentului este obligatoriu.");
        if (Date is null)
            errors.Add("Data documentului este obligatorie.");
        errors.AddRange(ContractUserControlViewModel.Validate());

        return errors;
    }

    public AdditionalDocument GetSelectedDocument()
    {
        if (SelectedDocument is null)
        {
            return new AdditionalDocument
            {
                Number = Number ?? 0,
                Date = Date.HasValue ? DateOnly.FromDateTime(Date.Value.DateTime) : DateOnly.FromDateTime(DateTime.Today)
            };
        }

        return SelectedDocument;
    }

    public async Task<AdditionalDocument> SaveAdditionalDocumentAsync()
    {
        var savedContract = await ContractUserControlViewModel.SaveContractAsync();

        if (Number == null || Number <= 0)
        {
            Number = await _documentService.GetNextNumberForContractAsync(savedContract.Id);
        }

        AdditionalDocument savedDocument;
        if (SelectedDocument is null)
        {
            savedDocument = GetSelectedDocument();
            savedDocument.Number = Number.Value;
            savedDocument.Contract = savedContract;
            savedDocument.ContractId = savedContract.Id;
            await _documentService.Add(savedDocument);
        }
        else
        {
            savedDocument = SelectedDocument;
            savedDocument.Number = Number.Value;
            if (Date.HasValue)
                savedDocument.Date = DateOnly.FromDateTime(Date.Value.DateTime);
            savedDocument.Contract = savedContract;
            savedDocument.ContractId = savedContract.Id;
            await _documentService.Update(savedDocument);
        }

        await _documentService.SubmitChanges();
        await LoadDocumentsAsync();
        SelectedDocument = FilteredDocuments.FirstOrDefault(d => d.Id == savedDocument.Id);
        AppLogger.LogInfo($"Successfully saved AdditionalDocument: Id={savedDocument.Id}, Number={savedDocument.Number}, Date={savedDocument.Date}");

        return savedDocument;
    }

    public async Task DeleteAdditionalDocumentAsync()
    {
        if (SelectedDocument is null)
            return;

        var selected = SelectedDocument;
        AppLogger.LogInfo($"Deleting AdditionalDocument: Id={selected.Id}, Number={selected.Number}");

        await _documentService.Delete(selected);
        await _documentService.SubmitChanges();

        ClearSelectedDocument();
        await LoadDocumentsAsync();
        AppLogger.LogInfo($"AdditionalDocument Id={selected.Id} deleted successfully.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ContractUserControlViewModel.PropertyChanged -= _contractHandler;
        ContractUserControlViewModel.ClientUserControlViewModel.PropertyChanged -= _clientHandler;

        ContractUserControlViewModel.Dispose();
    }
}
