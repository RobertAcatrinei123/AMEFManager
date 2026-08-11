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

public partial class ClientWindowViewModel : ViewModelBase
{
    private readonly ClientService _clientService;
    private readonly AddressService _addressService;
    private readonly PersonService _personService;

    public ClientUserControlViewModel ClientUserControlViewModel { get; }

    public ClientWindowViewModel()
        : this(App.Services.GetRequiredService<ClientService>(),
               App.Services.GetRequiredService<AddressService>(),
               App.Services.GetRequiredService<PersonService>())
    {
    
    }

    public ClientWindowViewModel(ClientService clientService, AddressService addressService, PersonService personService)
    {
        _clientService = clientService;
        _addressService = addressService;
        _personService = personService;
        ClientUserControlViewModel = new ClientUserControlViewModel(clientService, addressService, personService);
        ClientUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ClientUserControlViewModel.SelectedClient))
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
            var errors = ClientUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            ClientUserControlViewModel.IsLoading = true;

            var savedClient = await ClientUserControlViewModel.SaveClientAsync();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in ClientWindowViewModel.cs: {e}");
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
        var selected = ClientUserControlViewModel.SelectedClient;
        if (selected == null) return;
        
        var person = selected.Person;
        var address = selected.Address;
        var personAddress = person?.Address;
        
        await _clientService.Delete(selected);
        
        try
        {
            if (person != null) await _personService.Delete(person);
            if (address != null) await _addressService.Delete(address);
            if (personAddress != null) await _addressService.Delete(personAddress);
        }
        catch 
        {
        }
        
        await _clientService.SubmitChanges();
        ClientUserControlViewModel.ClearSelectedClientCommand.Execute(null);
        await ClientUserControlViewModel.LoadClientsAsync();
    }
}
