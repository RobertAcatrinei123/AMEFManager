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

public partial class ContractWindowViewModel : ViewModelBase
{
    public ContractUserControlViewModel ContractUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public ContractWindowViewModel()
        : this(App.Services.GetRequiredService<ContractService>(),
               App.Services.GetRequiredService<ContractTypeService>(),
               App.Services.GetRequiredService<ClientService>(),
               App.Services.GetRequiredService<AddressService>(),
               App.Services.GetRequiredService<PersonService>())
    {
    }

    public ContractWindowViewModel(ContractService contractService, ContractTypeService contractTypeService, ClientService clientService, AddressService addressService, PersonService personService)
    {
        ContractUserControlViewModel = new ContractUserControlViewModel(contractService, contractTypeService, clientService, addressService, personService);
        ContractUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public ContractWindowViewModel(ContractUserControlViewModel userControlViewModel)
    {
        ContractUserControlViewModel = userControlViewModel;
        ContractUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract))
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
            var errors = ContractUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            ContractUserControlViewModel.IsLoading = true;
            var savedContract = await ContractUserControlViewModel.SaveContractAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved Contract: Id={savedContract?.Id}, Number={savedContract?.Number}, Date={savedContract?.Date}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in ContractWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            ContractUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => ContractUserControlViewModel.SelectedContract != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (ContractUserControlViewModel.SelectedContract == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            ContractUserControlViewModel.IsLoading = true;
            await ContractUserControlViewModel.DeleteContractAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("Contract deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in ContractWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            ContractUserControlViewModel.IsLoading = false;
        }
    }
}
