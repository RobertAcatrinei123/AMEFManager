using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Services;
using AMEFManager.ViewModels.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AMEFManager.ViewModels.Windows;

public partial class AdditionalDocumentWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public AdditionalDocumentUserControlViewModel DocumentUserControlViewModel { get; }
    public AdditionalDocumentUserControlViewModel AdditionalDocumentUserControlViewModel => DocumentUserControlViewModel;

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public AdditionalDocumentWindowViewModel(
        AdditionalDocumentService documentService,
        ContractService contractService,
        ContractTypeService contractTypeService,
        ClientService clientService,
        AddressService addressService,
        PersonService personService)
        : this(new AdditionalDocumentUserControlViewModel(
            documentService,
            contractService,
            contractTypeService,
            clientService,
            addressService,
            personService))
    {
    }

    public AdditionalDocumentWindowViewModel(AdditionalDocumentUserControlViewModel userControlViewModel)
    {
        DocumentUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(DocumentUserControlViewModel.SelectedDocument))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        DocumentUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = DocumentUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            DocumentUserControlViewModel.IsLoading = true;
            var saved = await DocumentUserControlViewModel.SaveAdditionalDocumentAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved AdditionalDocument: Id={saved.Id}, Number={saved.Number}, Date={saved.Date}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in AdditionalDocumentWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
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
        if (DocumentUserControlViewModel.SelectedDocument == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            DocumentUserControlViewModel.IsLoading = true;
            await DocumentUserControlViewModel.DeleteAdditionalDocumentAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("AdditionalDocument deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in AdditionalDocumentWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            DocumentUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DocumentUserControlViewModel.PropertyChanged -= _childHandler;
        DocumentUserControlViewModel.Dispose();
    }
}
