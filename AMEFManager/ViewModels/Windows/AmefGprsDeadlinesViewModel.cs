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

namespace AMEFManager.ViewModels.Windows;

public partial class AmefGprsDeadlinesViewModel : ViewModelBase, IDisposable
{
    private readonly AmefService _amefService;
    private readonly Func<DateOnly>? _todayProvider;
    private DateOnly? _overrideReferenceDate;
    private List<Amef> _allAmefs = [];

    [ObservableProperty]
    private int _thresholdDays = 5;

    [ObservableProperty]
    private ObservableCollection<Amef> _filteredAmefs = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    public int DaysThreshold
    {
        get => ThresholdDays;
        set => ThresholdDays = value;
    }

    public ObservableCollection<Amef> ImpendingAmefs => FilteredAmefs;

    public DateOnly Today => _overrideReferenceDate ?? _todayProvider?.Invoke() ?? DateOnly.FromDateTime(DateTime.Today);

    public AmefGprsDeadlinesViewModel(AmefService amefService, Func<DateOnly>? todayProvider = null)
    {
        _amefService = amefService ?? throw new ArgumentNullException(nameof(amefService));
        _todayProvider = todayProvider;
        _ = LoadAmefsAsync();
    }

    partial void OnThresholdDaysChanged(int value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    public async Task LoadAmefsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            _allAmefs = await _amefService.FindAll();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to load AMEFs in AmefGprsDeadlinesViewModel: {ex.Message}", ex);
            ErrorMessage = $"Eroare la încărcarea aparatelor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ApplyFilter(DateOnly? referenceDate = null)
    {
        if (referenceDate.HasValue)
        {
            _overrideReferenceDate = referenceDate;
        }

        var baseDate = _overrideReferenceDate ?? _todayProvider?.Invoke() ?? DateOnly.FromDateTime(DateTime.Today);
        var maxDate = baseDate.AddDays(ThresholdDays);

        var filtered = _allAmefs
            .Where(a => a.Contract != null
                     && a.Contract.IsActive
                     && string.Equals(a.ConnectionMethod, "GPRS", StringComparison.OrdinalIgnoreCase)
                     && (!a.ConnectionExpirationDate.HasValue || a.ConnectionExpirationDate.Value <= maxDate))
            .OrderBy(a => a.ConnectionExpirationDate)
            .ThenBy(a => a.Series)
            .ToList();

        FilteredAmefs = new ObservableCollection<Amef>(filtered);
        OnPropertyChanged(nameof(ImpendingAmefs));
    }

    private static DateOnly GetEndOfMonth(DateOnly date)
    {
        int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        return new DateOnly(date.Year, date.Month, daysInMonth);
    }

    private async Task ExtendExpirationMonthsAsync(Amef amef, int months)
    {
        if (amef == null) return;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            var baseDate = amef.ConnectionExpirationDate ?? (_overrideReferenceDate ?? _todayProvider?.Invoke() ?? DateOnly.FromDateTime(DateTime.Today));
            var targetMonthDate = baseDate.AddMonths(months);
            amef.ConnectionExpirationDate = GetEndOfMonth(targetMonthDate);
            var existing = _allAmefs.FirstOrDefault(a => a.Id == amef.Id);
            if (existing != null && !ReferenceEquals(existing, amef))
            {
                existing.ConnectionExpirationDate = amef.ConnectionExpirationDate;
            }

            await _amefService.Update(amef);
            await _amefService.SubmitChanges();
            ApplyFilter();
            StatusMessage = $"Termenul GPRS pentru AMEF seria {amef.Series} a fost prelungit cu {months} {(months == 1 ? "lună" : "luni")} (până la {amef.ConnectionExpirationDate:dd.MM.yyyy}).";
            AppLogger.LogInfo($"AMEF {amef.Series} GPRS deadline extended by {months} months to {amef.ConnectionExpirationDate}.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to extend GPRS deadline for AMEF {amef.Series}: {ex.Message}", ex);
            ErrorMessage = $"Eroare la prelungirea termenului GPRS: {ex.Message}";
        }
    }

    [RelayCommand]
    public Task ExtendOneMonthAsync(Amef amef) => ExtendExpirationMonthsAsync(amef, 1);

    [RelayCommand]
    public Task ExtendFourMonthsAsync(Amef amef) => ExtendExpirationMonthsAsync(amef, 4);

    [RelayCommand]
    public Task ExtendTwelveMonthsAsync(Amef amef) => ExtendExpirationMonthsAsync(amef, 12);

    public void Dispose()
    {
        FilteredAmefs.Clear();
        _allAmefs.Clear();
    }
}
