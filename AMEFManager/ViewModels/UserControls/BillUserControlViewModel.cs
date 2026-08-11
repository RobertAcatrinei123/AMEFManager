using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AMEFManager.Models;
using AMEFManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.UserControls;

public partial class BillUserControlViewModel : ViewModelBase
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

        Debug.Print("Selected bill changed");

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
                BillDate = new DateTimeOffset(value.BillDate.ToDateTime(TimeOnly.MinValue));
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
                StartsWith(b.BillSeries, BillSeries) ||
                b.Equals(SelectedBill)
            ).OrderBy(x => x.Id).ToList();

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
}
