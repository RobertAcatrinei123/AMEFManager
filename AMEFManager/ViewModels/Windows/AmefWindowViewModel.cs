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

public partial class AmefWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public AmefUserControlViewModel AmefUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public AmefWindowViewModel(
        AmefService amefService,
        BillService billService,
        AddressService addressService,
        AuthorizationService authorizationService,
        ContractService contractService,
        ContractTypeService contractTypeService,
        ClientService clientService,
        PersonService personService)
        : this(new AmefUserControlViewModel(
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

    public AmefWindowViewModel(AmefUserControlViewModel userControlViewModel)
    {
        AmefUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(AmefUserControlViewModel.SelectedAmef))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
                UnassignContractCommand.NotifyCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(AmefUserControlViewModel.CanUnassignContract))
            {
                UnassignContractCommand.NotifyCanExecuteChanged();
            }
        };
        AmefUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = AmefUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            AmefUserControlViewModel.IsLoading = true;
            var savedAmef = await AmefUserControlViewModel.SaveAmefAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved AMEF: Id={savedAmef?.Id}, NUI={savedAmef?.NUI}, Series={savedAmef?.Series}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in AmefWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            AmefUserControlViewModel.IsLoading = false;
            UnassignContractCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanDelete() => AmefUserControlViewModel.SelectedAmef != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (AmefUserControlViewModel.SelectedAmef == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            AmefUserControlViewModel.IsLoading = true;
            await AmefUserControlViewModel.DeleteAmefAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("AMEF deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in AmefWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            AmefUserControlViewModel.IsLoading = false;
            UnassignContractCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanUnassignContract() => AmefUserControlViewModel.CanUnassignContract;

    [RelayCommand(CanExecute = nameof(CanUnassignContract))]
    private async Task UnassignContractAsync()
    {
        if (!CanUnassignContract()) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            AmefUserControlViewModel.IsLoading = true;
            await AmefUserControlViewModel.UnassignContractAsync();
            StatusMessage = "Aparatul AMEF a fost dezasociat de pe contract cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("AMEF unassigned from contract successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Unassign contract failed in AmefWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la dezasociere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A apărut o eroare la dezasociere:\n{msg}");
        }
        finally
        {
            AmefUserControlViewModel.IsLoading = false;
            UnassignContractCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AmefUserControlViewModel.PropertyChanged -= _childHandler;
        AmefUserControlViewModel.Dispose();
    }
}
