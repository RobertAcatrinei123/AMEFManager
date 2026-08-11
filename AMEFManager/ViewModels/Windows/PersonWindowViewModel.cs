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

public partial class PersonWindowViewModel : ViewModelBase
{
    private readonly PersonService _personService;
    private readonly AddressService _addressService;

    public PersonUserControlViewModel PersonUserControlViewModel { get; }

    public PersonWindowViewModel()
        : this(App.Services.GetRequiredService<PersonService>(),
               App.Services.GetRequiredService<AddressService>())
    {
    
    }

    public PersonWindowViewModel(PersonService personService, AddressService addressService)
    {
        _personService = personService;
        _addressService = addressService;
        PersonUserControlViewModel = new PersonUserControlViewModel(personService, addressService);
        PersonUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PersonUserControlViewModel.SelectedPerson))
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
            var errors = PersonUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            PersonUserControlViewModel.IsLoading = true;

            var addressVm = PersonUserControlViewModel.AddressUserControlViewModel;
            var address = await addressVm.SaveAddressAsync();

            Person savedPerson;

            if (PersonUserControlViewModel.SelectedPerson is null)
            {
                var existing = await _personService.FindByCnp(PersonUserControlViewModel.Cnp!) ?? 
                               await _personService.FindBySeriesAndNumber(PersonUserControlViewModel.Series!, PersonUserControlViewModel.Number!);
                
                if (existing != null)
                {
                    savedPerson = existing;
                    savedPerson.LastName = PersonUserControlViewModel.LastName!;
                    savedPerson.FirstName = PersonUserControlViewModel.FirstName!;
                    savedPerson.Cnp = PersonUserControlViewModel.Cnp!;
                    savedPerson.Email = PersonUserControlViewModel.Email;
                    savedPerson.Phone = PersonUserControlViewModel.Phone;
                    savedPerson.Series = PersonUserControlViewModel.Series!;
                    savedPerson.Number = PersonUserControlViewModel.Number!;
                    savedPerson.Issuer = PersonUserControlViewModel.Issuer!;
                    if (PersonUserControlViewModel.IssuingDate.HasValue)
                        savedPerson.IssuingDate = DateOnly.FromDateTime(PersonUserControlViewModel.IssuingDate.Value.DateTime);
                    savedPerson.Role = PersonUserControlViewModel.Role!;
                    savedPerson.Address = address;
                    savedPerson.AddressId = address.Id;
                    await _personService.Update(savedPerson);
                }
                else
                {
                    savedPerson = PersonUserControlViewModel.GetSelectedPerson();
                    savedPerson.Address = address;
                    savedPerson.AddressId = address.Id;
                    await _personService.Add(savedPerson);
                }
            }
            else
            {
                savedPerson = PersonUserControlViewModel.SelectedPerson;
                savedPerson.LastName = PersonUserControlViewModel.LastName!;
                savedPerson.FirstName = PersonUserControlViewModel.FirstName!;
                savedPerson.Cnp = PersonUserControlViewModel.Cnp!;
                savedPerson.Email = PersonUserControlViewModel.Email;
                savedPerson.Phone = PersonUserControlViewModel.Phone;
                savedPerson.Series = PersonUserControlViewModel.Series!;
                savedPerson.Number = PersonUserControlViewModel.Number!;
                savedPerson.Issuer = PersonUserControlViewModel.Issuer!;
                savedPerson.IssuingDate = DateOnly.FromDateTime(PersonUserControlViewModel.IssuingDate!.Value.DateTime);
                savedPerson.Role = PersonUserControlViewModel.Role!;
                savedPerson.Address = address;
                savedPerson.AddressId = address.Id;
            }

            await _personService.SubmitChanges();
            await PersonUserControlViewModel.LoadPersonsAsync();

            PersonUserControlViewModel.SelectedPerson =
                PersonUserControlViewModel.FilteredPersons.FirstOrDefault(p => p.Id == savedPerson.Id);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in PersonWindowViewModel.cs: {e}");
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            PersonUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => PersonUserControlViewModel.SelectedPerson != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        var selected = PersonUserControlViewModel.SelectedPerson;
        if (selected == null) return;
        
        var address = selected.Address;
        
        await _personService.Delete(selected);
        
        try
        {
            if (address != null) await _addressService.Delete(address);
        }
        catch 
        {
        }
        
        
        await _personService.SubmitChanges();
        PersonUserControlViewModel.ClearSelectedPersonCommand.Execute(null);
        await PersonUserControlViewModel.LoadPersonsAsync();
    }
}
