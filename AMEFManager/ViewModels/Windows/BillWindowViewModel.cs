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

public partial class BillWindowViewModel : ViewModelBase
{
    private readonly BillService _billService;

    public BillUserControlViewModel BillUserControlViewModel { get; }

    public BillWindowViewModel()
        : this(App.Services.GetRequiredService<BillService>())
    {
    
    }

    public BillWindowViewModel(BillService billService)
    {
        _billService = billService;
        BillUserControlViewModel = new BillUserControlViewModel(billService);
        BillUserControlViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(BillUserControlViewModel.SelectedBill))
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
            var errors = BillUserControlViewModel.Validate();
            if (errors.Count > 0)
            {
                await MessageBox.ShowWarning(string.Join("\n", errors), "Campuri obligatorii necompletate");
                return;
            }

            BillUserControlViewModel.IsLoading = true;
            Bill savedBill;

            if (BillUserControlViewModel.SelectedBill is null)
            {
                var existing = await _billService.FindBySeriesAndNumber(BillUserControlViewModel.BillSeries!, BillUserControlViewModel.BillNumber ?? 0);
                if (existing != null)
                {
                    savedBill = existing;
                    savedBill.BillDate = DateOnly.FromDateTime(BillUserControlViewModel.BillDate!.Value.DateTime);
                    await _billService.Update(savedBill);
                }
                else
                {
                    savedBill = BillUserControlViewModel.GetSelectedBill();
                    await _billService.Add(savedBill);
                }
            }
            else
            {
                savedBill = BillUserControlViewModel.SelectedBill;
                savedBill.BillDate = DateOnly.FromDateTime(BillUserControlViewModel.BillDate!.Value.DateTime);
                savedBill.BillSeries = BillUserControlViewModel.BillSeries!;
                savedBill.BillNumber = BillUserControlViewModel.BillNumber ?? 0;
            }

            await _billService.SubmitChanges();
            await BillUserControlViewModel.LoadBillsAsync();

            BillUserControlViewModel.SelectedBill =
                BillUserControlViewModel.FilteredBills.FirstOrDefault(b => b.Id == savedBill.Id);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Save failed in BillWindowViewModel.cs: {e}");
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
        var selected = BillUserControlViewModel.SelectedBill;
        if (selected == null) return;
        
        await _billService.Delete(selected);
        
        await _billService.SubmitChanges();
        BillUserControlViewModel.ClearSelectedBillCommand.Execute(null);
        await BillUserControlViewModel.LoadBillsAsync();
    }
}
