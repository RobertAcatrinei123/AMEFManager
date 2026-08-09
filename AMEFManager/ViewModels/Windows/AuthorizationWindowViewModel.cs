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

public partial class AuthorizationWindowViewModel : ViewModelBase
{
    private readonly AuthorizationService _authorizationService;

    public AuthorizationUserControlViewModel AuthorizationUserControlViewModel { get; }

    public AuthorizationWindowViewModel()
        : this(App.Services.GetRequiredService<AuthorizationService>())
    {
    
    }

    public AuthorizationWindowViewModel(AuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
        AuthorizationUserControlViewModel = new AuthorizationUserControlViewModel(authorizationService);
        AuthorizationUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AuthorizationUserControlViewModel.SelectedAuthorization))
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
            var errors = AuthorizationUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            AuthorizationUserControlViewModel.IsLoading = true;
            Authorization savedAuthorization;

            if (AuthorizationUserControlViewModel.SelectedAuthorization is null)
            {
                savedAuthorization = AuthorizationUserControlViewModel.GetSelectedAuthorization();
                await _authorizationService.Add(savedAuthorization);
            }
            else
            {
                savedAuthorization = AuthorizationUserControlViewModel.SelectedAuthorization;
                savedAuthorization.Number = AuthorizationUserControlViewModel.Number ?? 0;
                savedAuthorization.Date = DateOnly.FromDateTime(AuthorizationUserControlViewModel.Date!.Value.DateTime);
                savedAuthorization.Model = AuthorizationUserControlViewModel.Model!;
            }

            await _authorizationService.SubmitChanges();
            await AuthorizationUserControlViewModel.LoadAuthorizationsAsync();

            AuthorizationUserControlViewModel.SelectedAuthorization =
                AuthorizationUserControlViewModel.FilteredAuthorizations.FirstOrDefault(a => a.Id == savedAuthorization.Id);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in AuthorizationWindowViewModel.cs: {e}");
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
        var selected = AuthorizationUserControlViewModel.SelectedAuthorization;
        if (selected == null) return;
        
        await _authorizationService.Delete(selected);
        
        await _authorizationService.SubmitChanges();
        AuthorizationUserControlViewModel.ClearSelectedAuthorizationCommand.Execute(null);
        await AuthorizationUserControlViewModel.LoadAuthorizationsAsync();
    }
}
