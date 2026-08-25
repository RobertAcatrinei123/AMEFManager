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

public partial class PersonWindowViewModel : ViewModelBase, IDisposable
{
    private readonly PropertyChangedEventHandler _childHandler;
    private bool _disposed;

    public PersonUserControlViewModel PersonUserControlViewModel { get; }

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public PersonWindowViewModel(PersonService personService, AddressService addressService)
        : this(new PersonUserControlViewModel(personService, addressService))
    {
    }

    public PersonWindowViewModel(PersonUserControlViewModel userControlViewModel)
    {
        PersonUserControlViewModel = userControlViewModel;
        _childHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(PersonUserControlViewModel.SelectedPerson))
            {
                StatusMessage = null;
                ErrorMessage = null;
                DeleteCommand.NotifyCanExecuteChanged();
            }
        };
        PersonUserControlViewModel.PropertyChanged += _childHandler;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var errors = PersonUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            PersonUserControlViewModel.IsLoading = true;
            var savedPerson = await PersonUserControlViewModel.SavePersonAsync();
            StatusMessage = "Salvare realizată cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo($"Successfully saved Person: Id={savedPerson.Id}, Name={savedPerson.FirstName} {savedPerson.LastName}, CNP={savedPerson.Cnp}");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Save failed in PersonWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la salvare: {e.Message}";
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
        if (PersonUserControlViewModel.SelectedPerson == null) return;

        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            PersonUserControlViewModel.IsLoading = true;
            await PersonUserControlViewModel.DeletePersonAsync();
            StatusMessage = "Înregistrarea a fost ștearsă cu succes.";
            ErrorMessage = null;
            AppLogger.LogInfo("Person deleted successfully.");
        }
        catch (Exception e)
        {
            AppLogger.LogError($"Delete failed in PersonWindowViewModel: {e.Message}", e);
            StatusMessage = null;
            ErrorMessage = $"A apărut o eroare la ștergere: {e.Message}";
            var msg = e.Message;
            if (e.InnerException != null) msg += "\nInner: " + e.InnerException.Message;
            await MessageBox.ShowError($"A aparut o eroare la stergere:\n{msg}");
        }
        finally
        {
            PersonUserControlViewModel.IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        PersonUserControlViewModel.PropertyChanged -= _childHandler;
        PersonUserControlViewModel.Dispose();
    }
}
