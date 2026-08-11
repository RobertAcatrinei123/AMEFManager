using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Services;
using AMEFManager.ViewModels.UserControls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AMEFManager.ViewModels.Windows;

public partial class AdditionalDocumentWindowViewModel : ViewModelBase
{
    private readonly AdditionalDocumentService _documentService;
    private readonly ClientService _clientService;
    private readonly AddressService _addressService;
    private readonly PersonService _personService;

    public AdditionalDocumentUserControlViewModel DocumentUserControlViewModel { get; }

    public AdditionalDocumentWindowViewModel()
        : this(App.Services.GetRequiredService<AdditionalDocumentService>(),
               App.Services.GetRequiredService<ClientService>(),
               App.Services.GetRequiredService<AddressService>(),
               App.Services.GetRequiredService<PersonService>())
    {
    }

    public AdditionalDocumentWindowViewModel(AdditionalDocumentService documentService, ClientService clientService, AddressService addressService, PersonService personService)
    {
        _documentService = documentService;
        _clientService = clientService;
        _addressService = addressService;
        _personService = personService;
        DocumentUserControlViewModel = new AdditionalDocumentUserControlViewModel(documentService, clientService, addressService, personService);
        DocumentUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DocumentUserControlViewModel.SelectedDocument))
            {
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var errors = DocumentUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            DocumentUserControlViewModel.IsLoading = true;

            var clientVm = DocumentUserControlViewModel.ClientUserControlViewModel;
            var savedClient = await clientVm.SaveClientAsync();

            AdditionalDocument savedDocument;
            if (DocumentUserControlViewModel.SelectedDocument is null)
            {
                savedDocument = DocumentUserControlViewModel.GetSelectedDocument();
                savedDocument.Client = savedClient;
                savedDocument.ClientId = savedClient.Id;
                await _documentService.Add(savedDocument);
            }
            else
            {
                savedDocument = DocumentUserControlViewModel.SelectedDocument;
                savedDocument.Number = DocumentUserControlViewModel.Number ?? 0;
                savedDocument.Date = DateOnly.FromDateTime(DocumentUserControlViewModel.Date!.Value.DateTime);
                savedDocument.Client = savedClient;
                savedDocument.ClientId = savedClient.Id;
            }

            await _documentService.SubmitChanges();
            await DocumentUserControlViewModel.LoadDocumentsAsync();

            DocumentUserControlViewModel.SelectedDocument =
                DocumentUserControlViewModel.FilteredDocuments.FirstOrDefault(d => d.Id == savedDocument.Id);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in AdditionalDocumentWindowViewModel.cs: {e}");
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            DocumentUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => DocumentUserControlViewModel.SelectedDocument != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        var selected = DocumentUserControlViewModel.SelectedDocument;
        if (selected == null) return;
        
        await _documentService.Delete(selected);
        
        await _documentService.SubmitChanges();
        DocumentUserControlViewModel.ClearSelectedDocumentCommand.Execute(null);
        await DocumentUserControlViewModel.LoadDocumentsAsync();
    }
}
