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

public partial class DeliveryDocumentWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public DeliveryDocumentUserControlViewModel DeliveryDocumentUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public DeliveryDocumentWindowViewModel(
        DeliveryDocumentService deliveryDocumentService,
        AmefService amefService,
        BillService billService,
        AddressService addressService,
        AuthorizationService authorizationService,
        ContractService contractService,
        ContractTypeService contractTypeService,
        ClientService clientService,
        PersonService personService)
        : this(new DeliveryDocumentUserControlViewModel(
            deliveryDocumentService,
            amefService,
            billService,
            addressService,
            authorizationService,
            contractService,
            contractTypeService,
            clientService,
            personService))
    {
    }

    public DeliveryDocumentWindowViewModel(DeliveryDocumentUserControlViewModel userControlViewModel)
    {
        DeliveryDocumentUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(DeliveryDocumentUserControlViewModel.SelectedDeliveryDocument))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        DeliveryDocumentUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = DeliveryDocumentUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            DeliveryDocumentUserControlViewModel.IsLoading = true;
            var savedDeliveryDocument = await DeliveryDocumentUserControlViewModel.SaveDeliveryDocumentAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved DeliveryDocument: Id={savedDeliveryDocument?.Id}, Number={savedDeliveryDocument?.Number}, Date={savedDeliveryDocument?.Date}");
            await MessageBox.ShowInfo("Procesul verbal a fost salvat cu succes.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in DeliveryDocumentWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            DeliveryDocumentUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => DeliveryDocumentUserControlViewModel.SelectedDeliveryDocument != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (DeliveryDocumentUserControlViewModel.SelectedDeliveryDocument == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            DeliveryDocumentUserControlViewModel.IsLoading = true;
            await DeliveryDocumentUserControlViewModel.DeleteDeliveryDocumentAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("DeliveryDocument deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in DeliveryDocumentWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            DeliveryDocumentUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DeliveryDocumentUserControlViewModel.PropertyChanged -= _childHandler;
        DeliveryDocumentUserControlViewModel.Dispose();
    }
}
