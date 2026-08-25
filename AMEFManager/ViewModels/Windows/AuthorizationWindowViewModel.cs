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

public partial class AuthorizationWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public AuthorizationUserControlViewModel AuthorizationUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public AuthorizationWindowViewModel(AuthorizationService authorizationService)
        : this(new AuthorizationUserControlViewModel(authorizationService))
    {
    }

    public AuthorizationWindowViewModel(AuthorizationUserControlViewModel userControlViewModel)
    {
        AuthorizationUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(AuthorizationUserControlViewModel.SelectedAuthorization))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        AuthorizationUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = AuthorizationUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            AuthorizationUserControlViewModel.IsLoading = true;
            var savedAuthorization = await AuthorizationUserControlViewModel.SaveAuthorizationAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved Authorization: Id={savedAuthorization?.Id}, Number={savedAuthorization?.Number}, Model={savedAuthorization?.Model}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in AuthorizationWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la salvare:\n{msg}");
        }
        finally
        {
            AuthorizationUserControlViewModel.IsLoading = false;
        }
    }

    private bool CanDelete() => AuthorizationUserControlViewModel.SelectedAuthorization != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (AuthorizationUserControlViewModel.SelectedAuthorization == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            AuthorizationUserControlViewModel.IsLoading = true;
            await AuthorizationUserControlViewModel.DeleteAuthorizationAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("Authorization deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in AuthorizationWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            AuthorizationUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AuthorizationUserControlViewModel.PropertyChanged -= _childHandler;
        AuthorizationUserControlViewModel.Dispose();
    }
}
