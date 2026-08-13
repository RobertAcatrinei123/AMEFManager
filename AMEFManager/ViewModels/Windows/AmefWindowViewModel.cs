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

public partial class AmefWindowViewModel : ViewModelBase
{
    public AmefUserControlViewModel AmefUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public AmefWindowViewModel()
        : this(App.Services.GetRequiredService<AmefService>(),
               App.Services.GetRequiredService<BillService>(),
               App.Services.GetRequiredService<AddressService>(),
               App.Services.GetRequiredService<AuthorizationService>(),
               App.Services.GetRequiredService<ContractService>(),
               App.Services.GetRequiredService<ContractTypeService>(),
               App.Services.GetRequiredService<ClientService>(),
               App.Services.GetRequiredService<PersonService>())
    {
    }

    public AmefWindowViewModel(
        AmefService amefService,
        BillService billService,
        AddressService addressService,
        AuthorizationService authorizationService,
        ContractService contractService,
        ContractTypeService contractTypeService,
        ClientService clientService,
        PersonService personService)
    {
        AmefUserControlViewModel = new AmefUserControlViewModel(
            amefService, billService, addressService, authorizationService, contractService, contractTypeService, clientService, personService);
        AmefUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AmefUserControlViewModel.SelectedAmef))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public AmefWindowViewModel(AmefUserControlViewModel userControlViewModel)
    {
        AmefUserControlViewModel = userControlViewModel;
        AmefUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AmefUserControlViewModel.SelectedAmef))
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
        }
    }
}
