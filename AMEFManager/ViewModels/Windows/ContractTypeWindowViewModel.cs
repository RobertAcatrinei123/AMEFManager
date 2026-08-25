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

public partial class ContractTypeWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public ContractTypeUserControlViewModel ContractTypeUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public ContractTypeWindowViewModel(ContractTypeService contractTypeService)
        : this(new ContractTypeUserControlViewModel(contractTypeService))
    {
    }

    public ContractTypeWindowViewModel(ContractTypeUserControlViewModel userControlViewModel)
    {
        ContractTypeUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ContractTypeUserControlViewModel.SelectedContractType))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        ContractTypeUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = ContractTypeUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            ContractTypeUserControlViewModel.IsLoading = true;
            var saved = await ContractTypeUserControlViewModel.SaveContractTypeAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved ContractType: Id={saved.Id}, Name={saved.Name}, Value={saved.Value}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in ContractTypeWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            ContractTypeUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => ContractTypeUserControlViewModel.SelectedContractType != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (ContractTypeUserControlViewModel.SelectedContractType == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            ContractTypeUserControlViewModel.IsLoading = true;
            await ContractTypeUserControlViewModel.DeleteContractTypeAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("ContractType deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in ContractTypeWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            ContractTypeUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ContractTypeUserControlViewModel.PropertyChanged -= _childHandler;
        ContractTypeUserControlViewModel.Dispose();
    }
}
