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

public partial class ReasonWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public ReasonUserControlViewModel ReasonUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public ReasonWindowViewModel(ReasonService reasonService)
        : this(new ReasonUserControlViewModel(reasonService))
    {
    }

    public ReasonWindowViewModel(ReasonUserControlViewModel userControlViewModel)
    {
        ReasonUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ReasonUserControlViewModel.SelectedReason))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        ReasonUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = ReasonUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            ReasonUserControlViewModel.IsLoading = true;
            var saved = await ReasonUserControlViewModel.SaveReasonAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved Reason: Id={saved.Id}, Text={saved.Text}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in ReasonWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            ReasonUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => ReasonUserControlViewModel.SelectedReason != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (ReasonUserControlViewModel.SelectedReason == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            ReasonUserControlViewModel.IsLoading = true;
            await ReasonUserControlViewModel.DeleteReasonAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("Reason deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in ReasonWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            ReasonUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ReasonUserControlViewModel.PropertyChanged -= _childHandler;
        ReasonUserControlViewModel.Dispose();
    }
}
