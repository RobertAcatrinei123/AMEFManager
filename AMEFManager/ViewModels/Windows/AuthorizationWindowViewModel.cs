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

public partial class AuthorizationWindowViewModel : ViewModelBase
{
    public AuthorizationUserControlViewModel AuthorizationUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public AuthorizationWindowViewModel()
        : this(App.Services.GetRequiredService<AuthorizationService>())
    {
    }

    public AuthorizationWindowViewModel(AuthorizationService authorizationService)
    {
        AuthorizationUserControlViewModel = new AuthorizationUserControlViewModel(authorizationService);
        AuthorizationUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AuthorizationUserControlViewModel.SelectedAuthorization))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public AuthorizationWindowViewModel(AuthorizationUserControlViewModel userControlViewModel)
    {
        AuthorizationUserControlViewModel = userControlViewModel;
        AuthorizationUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AuthorizationUserControlViewModel.SelectedAuthorization))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
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
}
