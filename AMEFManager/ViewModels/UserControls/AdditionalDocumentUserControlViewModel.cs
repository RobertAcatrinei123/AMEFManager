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

public partial class AdditionalDocumentUserControlViewModel : ViewModelBase
{
    private readonly AdditionalDocumentService _documentService;
    private List<AdditionalDocument> _allDocuments = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public ClientUserControlViewModel ClientUserControlViewModel { get; }

    public AdditionalDocumentUserControlViewModel(AdditionalDocumentService documentService, ClientService clientService, AddressService addressService, PersonService personService)
    {
        _documentService = documentService;
        ClientUserControlViewModel = new ClientUserControlViewModel(clientService, addressService, personService);
        
        ClientUserControlViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient) && !_isUpdatingFromSelection) ApplyFilter(); };

        LoadDocumentsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<AdditionalDocument> _filteredDocuments = [];

    [ObservableProperty]
    private AdditionalDocument? _selectedDocument;

    [ObservableProperty]
    private int? _nr;

    [ObservableProperty]
    private DateTimeOffset? _date;

        [RelayCommand]
    private void ClearSelectedDocument()
    {
        _isUpdatingFromSelection = true;
        
        try
        {
            // Disable the automatic OnSelectedDocumentChanged logic
            _hasBeenFiltered = true;
            SelectedDocument = null;

            // Manually clear all fields
            Nr = null;
            Date = null;
            ClientUserControlViewModel.ClearSelectedClientCommand.Execute(null);
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

        Debug.Print("Selected document changed");

        _isUpdatingFromSelection = true;

        try
        {
            if (value is null)
            {
                Nr = null;
                Date = null;
                ClientUserControlViewModel.ClearSelectedClientCommand.Execute(null);
            }
            else
            {
                Nr = value.Nr;
                Date = new DateTimeOffset(value.Date.ToDateTime(TimeOnly.MinValue));

                var matchingClient = ClientUserControlViewModel.FilteredClients
                    .FirstOrDefault(c => c.Id == value.ClientId);
                ClientUserControlViewModel.SelectedClient = matchingClient;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnNrChanged(int? value) => OnFieldChanged();
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
                    var selectedClient = ClientUserControlViewModel.SelectedClient;

            var filtered = _allDocuments.Where(d =>
                (Nr == null || d.Nr == Nr) &&
                (selectedClient == null || d.ClientId == selectedClient.Id) ||
                d.Equals(SelectedDocument)
            ).OrderBy(x => x.Id).ToList();

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

        if (Nr == null || Nr <= 0)
            errors.Add("Numarul documentului este obligatoriu.");
        if (Date is null)
            errors.Add("Data documentului este obligatorie.");
        errors.AddRange(ClientUserControlViewModel.Validate());

        return errors;
    }

    public AdditionalDocument GetSelectedDocument()
    {
        if (SelectedDocument is null)
        {
            return new AdditionalDocument
            {
                Nr = Nr ?? 0,
                Date = DateOnly.FromDateTime(Date!.Value.DateTime)
            };
        }

        return SelectedDocument;
    }
}
