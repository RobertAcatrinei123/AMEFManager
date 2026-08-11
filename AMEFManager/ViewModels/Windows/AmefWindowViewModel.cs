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

public partial class AmefWindowViewModel : ViewModelBase
{
    private readonly AmefService _amefService;
    private readonly BillService _billService;
    private readonly AddressService _addressService;
    private readonly AuthorizationService _authorizationService;
    private readonly ContractService _contractService;
    private readonly ContractTypeService _contractTypeService;
    private readonly ClientService _clientService;
    private readonly PersonService _personService;

    public AmefUserControlViewModel AmefUserControlViewModel { get; }

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
        _amefService = amefService;
        _billService = billService;
        _addressService = addressService;
        _authorizationService = authorizationService;
        _contractService = contractService;
        _contractTypeService = contractTypeService;
        _clientService = clientService;
        _personService = personService;

        AmefUserControlViewModel = new AmefUserControlViewModel(
            amefService, billService, addressService, authorizationService, contractService, contractTypeService, clientService, personService);
        AmefUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AmefUserControlViewModel.SelectedAmef))
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
            var errors = AmefUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            AmefUserControlViewModel.IsLoading = true;

            var addressVm = AmefUserControlViewModel.AddressUserControlViewModel;
            var address = await addressVm.SaveAddressAsync();

            var billVm = AmefUserControlViewModel.BillUserControlViewModel;
            Bill savedBill;
            if (billVm.SelectedBill is null)
            {
                var existing = await _billService.FindBySeriesAndNumber(billVm.BillSeries!, billVm.BillNumber ?? 0);
                if (existing != null)
                {
                    savedBill = existing;
                    savedBill.BillDate = DateOnly.FromDateTime(billVm.BillDate!.Value.DateTime);
                    await _billService.Update(savedBill);
                }
                else
                {
                    savedBill = billVm.GetSelectedBill();
                    await _billService.Add(savedBill);
                }
            }
            else
            {
                savedBill = billVm.SelectedBill;
                savedBill.BillDate = DateOnly.FromDateTime(billVm.BillDate!.Value.DateTime);
                savedBill.BillSeries = billVm.BillSeries!;
                savedBill.BillNumber = billVm.BillNumber ?? 0;
            }
            await _billService.SubmitChanges();
            await billVm.LoadBillsAsync();
            billVm.SelectedBill = billVm.FilteredBills.FirstOrDefault(b => b.Id == savedBill.Id);

            var authVm = AmefUserControlViewModel.AuthorizationUserControlViewModel;
            Authorization savedAuth;
            if (authVm.SelectedAuthorization is null)
            {
                savedAuth = authVm.GetSelectedAuthorization();
                await _authorizationService.Add(savedAuth);
            }
            else
            {
                savedAuth = authVm.SelectedAuthorization;
                savedAuth.Number = authVm.Number ?? 0;
                savedAuth.Date = DateOnly.FromDateTime(authVm.Date!.Value.DateTime);
                savedAuth.Model = authVm.Model!;
            }
            await _authorizationService.SubmitChanges();
            await authVm.LoadAuthorizationsAsync();
            authVm.SelectedAuthorization = authVm.FilteredAuthorizations.FirstOrDefault(a => a.Id == savedAuth.Id);

            var contractVm = AmefUserControlViewModel.ContractUserControlViewModel;
            
            var typeVm = contractVm.ContractTypeUserControlViewModel;
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

            var clientVm = contractVm.ClientUserControlViewModel;
            var savedClient = await clientVm.SaveClientAsync();

            Contract savedContract;
            if (contractVm.SelectedContract is null)
            {
                var existing = await _contractService.FindByNumber(contractVm.Number ?? 0);
                if (existing != null)
                {
                    savedContract = existing;
                    savedContract.Date = DateOnly.FromDateTime(contractVm.Date!.Value.DateTime);
                    savedContract.IsActive = contractVm.IsActive;
                    savedContract.ValidUntil = contractVm.ValidUntil!.Value.DateTime;
                    savedContract.Type = savedType;
                    savedContract.ContractTypeId = savedType.Id;
                    savedContract.Client = savedClient;
                    savedContract.ClientId = savedClient.Id;
                    await _contractService.Update(savedContract);
                }
                else
                {
                    savedContract = contractVm.GetSelectedContract();
                    savedContract.Type = savedType;
                    savedContract.ContractTypeId = savedType.Id;
                    savedContract.Client = savedClient;
                    savedClient.PersonId = savedClient.PersonId; 
                    savedContract.ClientId = savedClient.Id;
                    await _contractService.Add(savedContract);
                }
            }
            else
            {
                savedContract = contractVm.SelectedContract;
                savedContract.Number = contractVm.Number ?? 0;
                savedContract.Date = DateOnly.FromDateTime(contractVm.Date!.Value.DateTime);
                savedContract.IsActive = contractVm.IsActive;
                savedContract.ValidUntil = contractVm.ValidUntil!.Value.DateTime;
                savedContract.Type = savedType;
                savedContract.ContractTypeId = savedType.Id;
                savedContract.Client = savedClient;
                savedContract.ClientId = savedClient.Id;
            }
            await _contractService.SubmitChanges();
            await contractVm.LoadContractsAsync();
            contractVm.SelectedContract = contractVm.FilteredContracts.FirstOrDefault(c => c.Id == savedContract.Id);


            Amef savedAmef;
            if (AmefUserControlViewModel.SelectedAmef is null)
            {
                var existing = await _amefService.FindByNui(AmefUserControlViewModel.Nui!) ?? await _amefService.FindBySeries(AmefUserControlViewModel.Series!);
                if (existing != null)
                {
                    savedAmef = existing;
                    savedAmef.Model = AmefUserControlViewModel.Model!;
                    savedAmef.Series = AmefUserControlViewModel.Series!;
                    savedAmef.NUI = AmefUserControlViewModel.Nui!;
                    savedAmef.FiscalCity = AmefUserControlViewModel.FiscalCity!;
                    savedAmef.FiscalizationDate = DateOnly.FromDateTime(AmefUserControlViewModel.FiscalizationDate!.Value.DateTime);
                    savedAmef.ConnectionMethod = AmefUserControlViewModel.ConnectionMethod;
                    savedAmef.ConnectionExpirationDate = AmefUserControlViewModel.ConnectionExpirationDate.HasValue
                        ? DateOnly.FromDateTime(AmefUserControlViewModel.ConnectionExpirationDate.Value.DateTime)
                        : null;
                    savedAmef.Address = address;
                    savedAmef.AddressId = address.Id;
                    savedAmef.Bill = savedBill;
                    savedAmef.BillId = savedBill.Id;
                    savedAmef.Authorization = savedAuth;
                    savedAmef.AuthorizationId = savedAuth.Id;
                    savedAmef.Contract = savedContract;
                    savedAmef.ContractId = savedContract.Id;
                    await _amefService.Update(savedAmef);
                }
                else
                {
                    savedAmef = AmefUserControlViewModel.GetSelectedAmef();
                    savedAmef.Address = address;
                    savedAmef.AddressId = address.Id;
                    savedAmef.Bill = savedBill;
                    savedAmef.BillId = savedBill.Id;
                    savedAmef.Authorization = savedAuth;
                    savedAmef.AuthorizationId = savedAuth.Id;
                    savedAmef.Contract = savedContract;
                    savedAmef.ContractId = savedContract.Id;
                    await _amefService.Add(savedAmef);
                }
            }
            else
            {
                savedAmef = AmefUserControlViewModel.SelectedAmef;
                savedAmef.Model = AmefUserControlViewModel.Model!;
                savedAmef.Series = AmefUserControlViewModel.Series!;
                savedAmef.NUI = AmefUserControlViewModel.Nui!;
                savedAmef.FiscalCity = AmefUserControlViewModel.FiscalCity!;
                savedAmef.FiscalizationDate = DateOnly.FromDateTime(AmefUserControlViewModel.FiscalizationDate!.Value.DateTime);
                savedAmef.ConnectionMethod = AmefUserControlViewModel.ConnectionMethod;
                savedAmef.ConnectionExpirationDate = AmefUserControlViewModel.ConnectionExpirationDate.HasValue
                    ? DateOnly.FromDateTime(AmefUserControlViewModel.ConnectionExpirationDate.Value.DateTime)
                    : null;
                    
                savedAmef.Address = address;
                savedAmef.AddressId = address.Id;
                savedAmef.Bill = savedBill;
                savedAmef.BillId = savedBill.Id;
                savedAmef.Authorization = savedAuth;
                savedAmef.AuthorizationId = savedAuth.Id;
                savedAmef.Contract = savedContract;
                savedAmef.ContractId = savedContract.Id;
            }

            await _amefService.SubmitChanges();
            await AmefUserControlViewModel.LoadAmefsAsync();

            AmefUserControlViewModel.SelectedAmef =
                AmefUserControlViewModel.FilteredAmefs.FirstOrDefault(a => a.Id == savedAmef.Id);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in AmefWindowViewModel.cs: {e}");
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
        var selected = AmefUserControlViewModel.SelectedAmef;
        if (selected == null) return;
        
        var address = selected.Address;
        var bill = selected.Bill;
        var auth = selected.Authorization;
        
        await _amefService.Delete(selected);
        
        try
        {
            if (address != null) await _addressService.Delete(address);
            if (bill != null) await _billService.Delete(bill);
            if (auth != null) await _authorizationService.Delete(auth);
        }
        catch 
        {
        
        }
        await _amefService.SubmitChanges();
        AmefUserControlViewModel.ClearSelectedAmefCommand.Execute(null);
        await AmefUserControlViewModel.LoadAmefsAsync();
    }
}
