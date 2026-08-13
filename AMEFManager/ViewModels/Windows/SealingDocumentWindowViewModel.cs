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

public partial class SealingDocumentWindowViewModel : ViewModelBase
{
    public SealingDocumentUserControlViewModel SealingDocumentUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public SealingDocumentWindowViewModel()
        : this(App.Services.GetRequiredService<SealingDocumentService>(),
               App.Services.GetRequiredService<AmefService>(),
               App.Services.GetRequiredService<BillService>(),
               App.Services.GetRequiredService<AddressService>(),
               App.Services.GetRequiredService<AuthorizationService>(),
               App.Services.GetRequiredService<ContractService>(),
               App.Services.GetRequiredService<ContractTypeService>(),
               App.Services.GetRequiredService<ClientService>(),
               App.Services.GetRequiredService<PersonService>())
    {
    }

    public SealingDocumentWindowViewModel(
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
        SealingDocumentUserControlViewModel = new SealingDocumentUserControlViewModel(
            sealingDocumentService, amefService, billService, addressService, authorizationService, contractService, contractTypeService, clientService, personService);
        SealingDocumentUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SealingDocumentUserControlViewModel.SelectedSealingDocument))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public SealingDocumentWindowViewModel(SealingDocumentUserControlViewModel userControlViewModel)
    {
        SealingDocumentUserControlViewModel = userControlViewModel;
        SealingDocumentUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SealingDocumentUserControlViewModel.SelectedSealingDocument))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = SealingDocumentUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            SealingDocumentUserControlViewModel.IsLoading = true;
            var savedSealingDocument = await SealingDocumentUserControlViewModel.SaveSealingDocumentAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved SealingDocument: Id={savedSealingDocument?.Id}, Number={savedSealingDocument?.Number}, Date={savedSealingDocument?.Date}");
            await MessageBox.ShowInfo("Documentul de sigilare a fost salvat cu succes.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in SealingDocumentWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            SealingDocumentUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => SealingDocumentUserControlViewModel.SelectedSealingDocument != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (SealingDocumentUserControlViewModel.SelectedSealingDocument == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            SealingDocumentUserControlViewModel.IsLoading = true;
            await SealingDocumentUserControlViewModel.DeleteSealingDocumentAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("SealingDocument deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in SealingDocumentWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            SealingDocumentUserControlViewModel.IsLoading = false;
        }
    }
}
