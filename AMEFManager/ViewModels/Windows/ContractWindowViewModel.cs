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

public partial class ContractWindowViewModel : ViewModelBase
{
    private readonly ContractService _contractService;
    private readonly ContractTypeService _contractTypeService;
    private readonly ClientService _clientService;
    private readonly AddressService _addressService;
    private readonly PersonService _personService;

    public ContractUserControlViewModel ContractUserControlViewModel { get; }

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
        _contractService = contractService;
        _contractTypeService = contractTypeService;
        _clientService = clientService;
        _addressService = addressService;
        _personService = personService;
        ContractUserControlViewModel = new ContractUserControlViewModel(contractService, contractTypeService, clientService, addressService, personService);
        ContractUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContractUserControlViewModel.SelectedContract))
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
            var errors = ContractUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            ContractUserControlViewModel.IsLoading = true;

            var typeVm = ContractUserControlViewModel.ContractTypeUserControlViewModel;
            ContractType savedType;
            
            if (typeVm.SelectedContractType is null)
            {
                savedType = typeVm.GetSelectedContractType();
                await _contractTypeService.Add(savedType);
            }
            else
            {
                savedType = typeVm.SelectedContractType;
                savedType.Name = typeVm.Name!;
                savedType.Value = typeVm.Value ?? 0;
            }
            await _contractTypeService.SubmitChanges();
            await typeVm.LoadContractTypesAsync();
            typeVm.SelectedContractType = typeVm.FilteredContractTypes.FirstOrDefault(t => t.Id == savedType.Id);

            var clientVm = ContractUserControlViewModel.ClientUserControlViewModel;
            var savedClient = await clientVm.SaveClientAsync();

            Contract savedContract;
            if (ContractUserControlViewModel.SelectedContract is null)
            {
                var existing = await _contractService.FindByNumber(ContractUserControlViewModel.Number ?? 0);
                if (existing != null)
                {
                    savedContract = existing;
                    savedContract.Date = DateOnly.FromDateTime(ContractUserControlViewModel.Date!.Value.DateTime);
                    savedContract.IsActive = ContractUserControlViewModel.IsActive;
                    savedContract.ValidUntil = ContractUserControlViewModel.ValidUntil!.Value.DateTime;
                    savedContract.Type = savedType;
                    savedContract.ContractTypeId = savedType.Id;
                    savedContract.Client = savedClient;
                    savedContract.ClientId = savedClient.Id;
                    await _contractService.Update(savedContract);
                }
                else
                {
                    savedContract = ContractUserControlViewModel.GetSelectedContract();
                    savedContract.Type = savedType;
                    savedContract.ContractTypeId = savedType.Id;
                    savedContract.Client = savedClient;
                    savedContract.ClientId = savedClient.Id;
                    await _contractService.Add(savedContract);
                }
            }
            else
            {
                savedContract = ContractUserControlViewModel.SelectedContract;
                savedContract.Number = ContractUserControlViewModel.Number ?? 0;
                savedContract.Date = DateOnly.FromDateTime(ContractUserControlViewModel.Date!.Value.DateTime);
                savedContract.IsActive = ContractUserControlViewModel.IsActive;
                savedContract.ValidUntil = ContractUserControlViewModel.ValidUntil!.Value.DateTime;
                savedContract.Type = savedType;
                savedContract.ContractTypeId = savedType.Id;
                savedContract.Client = savedClient;
                savedContract.ClientId = savedClient.Id;
            }

            await _contractService.SubmitChanges();
            await ContractUserControlViewModel.LoadContractsAsync();

            ContractUserControlViewModel.SelectedContract =
                ContractUserControlViewModel.FilteredContracts.FirstOrDefault(c => c.Id == savedContract.Id);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in ContractWindowViewModel.cs: {e}");
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
        var selected = ContractUserControlViewModel.SelectedContract;
        if (selected == null) return;
        
        await _contractService.Delete(selected);
        
        await _contractService.SubmitChanges();
        ContractUserControlViewModel.ClearSelectedContractCommand.Execute(null);
        await ContractUserControlViewModel.LoadContractsAsync();
    }
}
