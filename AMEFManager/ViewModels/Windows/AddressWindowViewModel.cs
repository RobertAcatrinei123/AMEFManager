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

public partial class AddressWindowViewModel : ViewModelBase
{
    private readonly AddressService _addressService;

    public AddressUserControlViewModel AddressUserControlViewModel { get; }

    public AddressWindowViewModel()
        : this(App.Services.GetRequiredService<AddressService>())
    {
    
    }

    public AddressWindowViewModel(AddressService addressService)
    {
        _addressService = addressService;
        AddressUserControlViewModel = new AddressUserControlViewModel(addressService);
        AddressUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AddressUserControlViewModel.SelectedAddress))
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
            AddressUserControlViewModel.IsLoading = true;
            await AddressUserControlViewModel.SaveAddressAsync();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in AddressWindowViewModel.cs: {e}");
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            AddressUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => AddressUserControlViewModel.SelectedAddress != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        var selected = AddressUserControlViewModel.SelectedAddress;
        if (selected == null) return;
        
        await _addressService.Delete(selected);
        
        await _addressService.SubmitChanges();
        AddressUserControlViewModel.ClearSelectedAddressCommand.Execute(null);
        await AddressUserControlViewModel.LoadAddressesAsync();
    }
}