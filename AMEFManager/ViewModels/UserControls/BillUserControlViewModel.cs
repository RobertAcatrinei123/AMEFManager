using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.UserControls;

public partial class BillUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly BillService _billService;
    private List<Bill> _allBills = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public BillUserControlViewModel(BillService billService)
    {
        _billService = billService;
        LoadBillsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Factură Achiziție";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Bill> _filteredBills = [];

    [ObservableProperty]
    private Bill? _selectedBill;

    [ObservableProperty]
    private DateTimeOffset? _billDate;

    [ObservableProperty]
    private string? _billSeries;

    [ObservableProperty]
    private int? _billNumber;

    [RelayCommand]
    private void ClearSelectedBill()
    {
        try
        {
            _isUpdatingFromSelection = true;
            
            _hasBeenFiltered = true;
            SelectedBill = null;

            BillDate = null;
            BillSeries = null;
            BillNumber = null;
        }
        finally
        {
            _isUpdatingFromSelection = false;
            ApplyFilter();
            _hasBeenFiltered = false;
        }
    }

    [RelayCommand]
    public async Task LoadBillsAsync()
    {
        IsLoading = true;
        _allBills = await _billService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedBillChanged(Bill? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected Bill changed: Id={value?.Id}, Series={value?.BillSeries}, Number={value?.BillNumber}");

        try
        {
            _isUpdatingFromSelection = true;

            if (value is null)
            {
                BillDate = null;
                BillSeries = null;
                BillNumber = null;
            }
            else
            {
                BillDate = DateHelper.ToDateTimeOffset(value.BillDate);
                BillSeries = value.BillSeries;
                BillNumber = value.BillNumber;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnBillSeriesChanged(string? value) => OnFieldChanged();

    partial void OnBillDateChanged(DateTimeOffset? value) => OnFieldChanged();
    partial void OnBillNumberChanged(int? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedBill is not null)
            return;

        if (_isUpdatingFromSelection)
            return;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _hasBeenFiltered = true;
        try
        {
            var filtered = _allBills.Where(b =>
                (BillNumber == null || b.BillNumber == BillNumber) &&
                StartsWith(b.BillSeries, BillSeries) ||
                b.Equals(SelectedBill)
            ).OrderByDescending(x => x.Id).ToList();

            FilteredBills = new ObservableCollection<Bill>(filtered);
        }
        finally
        {
            _hasBeenFiltered = false;
        }
    }

    private static bool StartsWith(string? value, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (BillDate is null)
            errors.Add("Data facturii este obligatorie.");
        if (string.IsNullOrWhiteSpace(BillSeries))
            errors.Add("Seria facturii este obligatorie.");
        if (BillNumber == null || BillNumber <= 0)
            errors.Add("Numarul facturii este obligatoriu.");

        return errors;
    }

    public Bill GetSelectedBill()
    {
        if (SelectedBill is null)
        {
            return new Bill
            {
                BillDate = DateOnly.FromDateTime(BillDate!.Value.DateTime),
                BillSeries = BillSeries!,
                BillNumber = BillNumber ?? 0,
            };
        }

        return SelectedBill;
    }

    public async Task<Bill> SaveBillAsync()
    {
        Bill savedBill;

        if (SelectedBill is null)
        {
            var existing = await _billService.FindBySeriesAndNumber(BillSeries!, BillNumber ?? 0);
            if (existing != null)
            {
                savedBill = existing;
                if (BillDate.HasValue)
                    savedBill.BillDate = DateOnly.FromDateTime(BillDate.Value.DateTime);
                savedBill.BillSeries = BillSeries!;
                savedBill.BillNumber = BillNumber ?? 0;
                await _billService.Update(savedBill);
            }
            else
            {
                savedBill = GetSelectedBill();
                await _billService.Add(savedBill);
            }
        }
        else
        {
            savedBill = SelectedBill;
            if (BillDate.HasValue)
                savedBill.BillDate = DateOnly.FromDateTime(BillDate.Value.DateTime);
            savedBill.BillSeries = BillSeries!;
            savedBill.BillNumber = BillNumber ?? 0;
            await _billService.Update(savedBill);
        }

        await _billService.SubmitChanges();
        await LoadBillsAsync();
        SelectedBill = FilteredBills.FirstOrDefault(b => b.Id == savedBill.Id);
        AppLogger.LogInfo($"Successfully saved Bill: Id={savedBill.Id}, Series={savedBill.BillSeries}, Number={savedBill.BillNumber}");

        return savedBill;
    }

    public async Task DeleteBillAsync()
    {
        if (SelectedBill is null)
            return;

        var toDelete = SelectedBill;
        AppLogger.LogInfo($"Deleting Bill: Id={toDelete.Id}, Series={toDelete.BillSeries}, Number={toDelete.BillNumber}");

        await _billService.Delete(toDelete);
        await _billService.SubmitChanges();

        ClearSelectedBill();
        await LoadBillsAsync();
        AppLogger.LogInfo($"Bill Id={toDelete.Id} deleted successfully.");
    }

    public void Dispose()
    {
    }
}
