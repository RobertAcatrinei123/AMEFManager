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

public partial class ContractTypeWindowViewModel : ViewModelBase
{
    private readonly ContractTypeService _contractTypeService;

    public ContractTypeUserControlViewModel ContractTypeUserControlViewModel { get; }

    public ContractTypeWindowViewModel()
        : this(App.Services.GetRequiredService<ContractTypeService>())
    {
    
    }

    public ContractTypeWindowViewModel(ContractTypeService contractTypeService)
    {
        _contractTypeService = contractTypeService;
        ContractTypeUserControlViewModel = new ContractTypeUserControlViewModel(contractTypeService);
        ContractTypeUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContractTypeUserControlViewModel.SelectedContractType))
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
            var errors = ContractTypeUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            ContractTypeUserControlViewModel.IsLoading = true;
            ContractType savedContractType;

            if (ContractTypeUserControlViewModel.SelectedContractType is null)
            {
                savedContractType = ContractTypeUserControlViewModel.GetSelectedContractType();
                await _contractTypeService.Add(savedContractType);
            }
            else
            {
                savedContractType = ContractTypeUserControlViewModel.SelectedContractType;
                savedContractType.Name = ContractTypeUserControlViewModel.Name!;
                savedContractType.Value = ContractTypeUserControlViewModel.Value ?? 0;
            }

            await _contractTypeService.SubmitChanges();
            await ContractTypeUserControlViewModel.LoadContractTypesAsync();

            ContractTypeUserControlViewModel.SelectedContractType =
                ContractTypeUserControlViewModel.FilteredContractTypes.FirstOrDefault(c => c.Id == savedContractType.Id);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in ContractTypeWindowViewModel.cs: {e}");
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
        var selected = ContractTypeUserControlViewModel.SelectedContractType;
        if (selected == null) return;
        
        await _contractTypeService.Delete(selected);
        
        await _contractTypeService.SubmitChanges();
        ContractTypeUserControlViewModel.ClearSelectedContractTypeCommand.Execute(null);
        await ContractTypeUserControlViewModel.LoadContractTypesAsync();
    }
}
