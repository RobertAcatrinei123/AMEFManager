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

public partial class BillWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public BillUserControlViewModel BillUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public BillWindowViewModel(BillService billService)
        : this(new BillUserControlViewModel(billService))
    {
    }

    public BillWindowViewModel(BillUserControlViewModel userControlViewModel)
    {
        BillUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(BillUserControlViewModel.SelectedBill))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        BillUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = BillUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            BillUserControlViewModel.IsLoading = true;
            var savedBill = await BillUserControlViewModel.SaveBillAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved Bill: Id={savedBill?.Id}, Series={savedBill?.BillSeries}, Number={savedBill?.BillNumber}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in BillWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            BillUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => BillUserControlViewModel.SelectedBill != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (BillUserControlViewModel.SelectedBill == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            BillUserControlViewModel.IsLoading = true;
            await BillUserControlViewModel.DeleteBillAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("Bill deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in BillWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            BillUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        BillUserControlViewModel.PropertyChanged -= _childHandler;
        BillUserControlViewModel.Dispose();
    }
}
