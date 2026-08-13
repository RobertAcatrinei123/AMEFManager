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

public partial class ClientWindowViewModel : ViewModelBase
{
    public ClientUserControlViewModel ClientUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public ClientWindowViewModel()
        : this(App.Services.GetRequiredService<ClientService>(),
               App.Services.GetRequiredService<AddressService>(),
               App.Services.GetRequiredService<PersonService>())
    {
    }

    public ClientWindowViewModel(ClientService clientService, AddressService addressService, PersonService personService)
    {
        ClientUserControlViewModel = new ClientUserControlViewModel(clientService, addressService, personService);
        ClientUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public ClientWindowViewModel(ClientUserControlViewModel userControlViewModel)
    {
        ClientUserControlViewModel = userControlViewModel;
        ClientUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient))
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
            var errors = ClientUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            ClientUserControlViewModel.IsLoading = true;
            var savedClient = await ClientUserControlViewModel.SaveClientAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved Client: Id={savedClient?.Id}, Name={savedClient?.Name}, NationalIdentifier={savedClient?.NationalIdentifier}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in ClientWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            ClientUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => ClientUserControlViewModel.SelectedClient != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (ClientUserControlViewModel.SelectedClient == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            ClientUserControlViewModel.IsLoading = true;
            await ClientUserControlViewModel.DeleteClientAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("Client deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in ClientWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            ClientUserControlViewModel.IsLoading = false;
        }
    }
}
