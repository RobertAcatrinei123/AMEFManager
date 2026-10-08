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
    private ObservableCollection<Amef> _deactivatedAmefs = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    private readonly IClipboardService _clipboardService;

    public int DaysThreshold
    {
        get => ThresholdDays;
        set => ThresholdDays = value;
    }

    public ObservableCollection<Amef> ImpendingAmefs => FilteredAmefs;

    public DateOnly Today => _overrideReferenceDate ?? _todayProvider?.Invoke() ?? DateOnly.FromDateTime(DateTime.Today);

    public AmefGprsDeadlinesViewModel(
        AmefService amefService,
        Func<DateOnly>? todayProvider = null,
        IClipboardService? clipboardService = null)
    {
        _amefService = amefService ?? throw new ArgumentNullException(nameof(amefService));
        _todayProvider = todayProvider;
        _clipboardService = clipboardService ?? new AvaloniaClipboardService();
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
            .Where(a => a.IsActive
                     && a.Contract != null
                     && a.Contract.IsActive
                     && string.Equals(a.ConnectionMethod, "GPRS", StringComparison.OrdinalIgnoreCase)
                     && (!a.ConnectionExpirationDate.HasValue || a.ConnectionExpirationDate.Value <= maxDate))
            .OrderBy(a => a.ConnectionExpirationDate)
            .ThenBy(a => a.Series)
            .ToList();

        var deactivated = _allAmefs
            .Where(a => !a.IsActive
                     && a.Contract != null
                     && a.Contract.IsActive
                     && string.Equals(a.ConnectionMethod, "GPRS", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.ConnectionExpirationDate)
            .ThenBy(a => a.Series)
            .ToList();

        FilteredAmefs = new ObservableCollection<Amef>(filtered);
        DeactivatedAmefs = new ObservableCollection<Amef>(deactivated);
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

    [RelayCommand]
    public async Task DeactivateAmefAsync(Amef amef)
    {
        if (amef == null) return;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            amef.IsActive = false;
            var existing = _allAmefs.FirstOrDefault(a => a.Id == amef.Id);
            if (existing != null && !ReferenceEquals(existing, amef))
            {
                existing.IsActive = false;
            }

            await _amefService.Update(amef);
            await _amefService.SubmitChanges();
            ApplyFilter();
            StatusMessage = $"Aparatul AMEF seria {amef.Series} a fost dezactivat cu succes.";
            AppLogger.LogInfo($"AMEF {amef.Series} deactivated via AmefGprsDeadlines.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to deactivate AMEF {amef.Series}: {ex.Message}", ex);
            ErrorMessage = $"Eroare la dezactivarea aparatului AMEF: {ex.Message}";
        }
    }

    private async Task ActivateAmefForMonthsAsync(Amef amef, int totalMonths)
    {
        if (amef == null) return;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            amef.IsActive = true;
            var firstOfCurrentMonth = new DateOnly(Today.Year, Today.Month, 1);
            var targetMonth = firstOfCurrentMonth.AddMonths(totalMonths - 1);
            amef.ConnectionExpirationDate = GetEndOfMonth(targetMonth);
            var existing = _allAmefs.FirstOrDefault(a => a.Id == amef.Id);
            if (existing != null && !ReferenceEquals(existing, amef))
            {
                existing.IsActive = true;
                existing.ConnectionExpirationDate = amef.ConnectionExpirationDate;
            }

            await _amefService.Update(amef);
            await _amefService.SubmitChanges();
            ApplyFilter();
            StatusMessage = $"Aparatul AMEF seria {amef.Series} a fost reactivat cu succes (expirare GPRS: {amef.ConnectionExpirationDate:dd.MM.yyyy}).";
            AppLogger.LogInfo($"AMEF {amef.Series} reactivated for {totalMonths} months via AmefGprsDeadlines to {amef.ConnectionExpirationDate}.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to activate AMEF {amef.Series}: {ex.Message}", ex);
            ErrorMessage = $"Eroare la activarea aparatului AMEF: {ex.Message}";
        }
    }

    [RelayCommand]
    public Task ActivateOneMonthAsync(Amef amef) => ActivateAmefForMonthsAsync(amef, 1);

    [RelayCommand]
    public Task ActivateFourMonthsAsync(Amef amef) => ActivateAmefForMonthsAsync(amef, 4);

    [RelayCommand]
    public Task ActivateTwelveMonthsAsync(Amef amef) => ActivateAmefForMonthsAsync(amef, 12);

    [RelayCommand]
    public Task ActivateAmefAsync(Amef amef) => ActivateOneMonthAsync(amef);

    public string GenerateExportText()
    {
        if (FilteredAmefs.Count == 0)
        {
            return string.Empty;
        }

        var clientGroups = FilteredAmefs
            .GroupBy(
                a => !string.IsNullOrWhiteSpace(a.Contract?.Client?.Name)
                    ? a.Contract.Client.Name.Trim()
                    : (a.Contract != null ? $"Contract {a.Contract.Number}" : "Fara client"),
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lines = new List<string>();
        foreach (var group in clientGroups)
        {
            var clientName = group.Key;
            var amefCount = group.Count();

            lines.Add($"{clientName} {amefCount} amef");
        }

        return string.Join("\n", lines);
    }

    [RelayCommand]
    public async Task ExportToClipboardAsync()
    {
        var text = GenerateExportText();
        if (string.IsNullOrEmpty(text))
        {
            StatusMessage = "Nu există aparate AMEF în listă pentru export.";
            return;
        }

        try
        {
            await _clipboardService.SetTextAsync(text);
            var clientCount = FilteredAmefs
                .Select(a => a.Contract?.Client?.Name?.Trim() ?? "Fara client")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            StatusMessage = $"{FilteredAmefs.Count} aparate AMEF ({clientCount} clienți) exportate în clipboard.";
            AppLogger.LogInfo($"Exported {FilteredAmefs.Count} AMEFs across {clientCount} clients to clipboard.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to export AMEFs to clipboard: {ex.Message}", ex);
            ErrorMessage = $"Eroare la exportul în clipboard: {ex.Message}";
        }
    }

    public void Dispose()
    {
        FilteredAmefs.Clear();
        DeactivatedAmefs.Clear();
        _allAmefs.Clear();
    }
}
