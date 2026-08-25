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

public partial class AddressWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public AddressUserControlViewModel AddressUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public AddressWindowViewModel(AddressService addressService)
        : this(new AddressUserControlViewModel(addressService))
    {
    }

    public AddressWindowViewModel(AddressUserControlViewModel userControlViewModel)
    {
        AddressUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(AddressUserControlViewModel.SelectedAddress))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        AddressUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = AddressUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            AddressUserControlViewModel.IsLoading = true;
            var saved = await AddressUserControlViewModel.SaveAddressAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved Address: Id={saved?.Id}, City={saved?.City}, Street={saved?.Street}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in AddressWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
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
        if (AddressUserControlViewModel.SelectedAddress == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            AddressUserControlViewModel.IsLoading = true;
            await AddressUserControlViewModel.DeleteAddressAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("Address deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in AddressWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            AddressUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AddressUserControlViewModel.PropertyChanged -= _childHandler;
        AddressUserControlViewModel.Dispose();
    }
}