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

public partial class ReasonUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly ReasonService _reasonService;
    private List<Reason> _allReasons = [];
    private bool _isUpdatingFromSelection;
    private bool _hasBeenFiltered;

    public ReasonUserControlViewModel(ReasonService reasonService)
    {
        _reasonService = reasonService;
        LoadReasonsCommand.Execute(null);
        _hasBeenFiltered = false;
    }

    [ObservableProperty]
    private string _headerTitle = "Date Motiv Refiscalizare";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<Reason> _filteredReasons = [];

    [ObservableProperty]
    private Reason? _selectedReason;

    [ObservableProperty]
    private string? _text;

    [RelayCommand]
    private void ClearSelectedReason()
    {
        _isUpdatingFromSelection = true;
        
        try
        {
            _hasBeenFiltered = true;
            SelectedReason = null;
            Text = null;
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }

        ApplyFilter();
        _hasBeenFiltered = false;
    }

    [RelayCommand]
    public async Task LoadReasonsAsync()
    {
        IsLoading = true;
        _allReasons = await _reasonService.FindAll();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnSelectedReasonChanged(Reason? value)
    {
        if (_hasBeenFiltered)
        {
            return;
        }

        AppLogger.LogDebug($"Selected Reason changed: Id={value?.Id}, Text={value?.Text}");

        _isUpdatingFromSelection = true;

        try
        {
            if (value is null)
            {
                Text = null;
            }
            else
            {
                Text = value.Text;
            }
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }
    }

    partial void OnTextChanged(string? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (SelectedReason is not null)
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
            var filtered = _allReasons.Where(r =>
                StartsWith(r.Text, Text) ||
                r.Equals(SelectedReason)
            ).OrderByDescending(x => x.Id).ToList();

            FilteredReasons = new ObservableCollection<Reason>(filtered);
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

        if (string.IsNullOrWhiteSpace(Text))
            errors.Add("Textul motivului de refiscalizare este obligatoriu.");

        return errors;
    }

    public Reason GetSelectedReason()
    {
        if (SelectedReason is null)
        {
            return new Reason
            {
                Text = Text?.Trim() ?? string.Empty
            };
        }

        return SelectedReason;
    }

    public async Task<Reason> SaveReasonAsync()
    {
        Reason savedReason;
        string cleanText = Text?.Trim() ?? string.Empty;

        if (SelectedReason is null)
        {
            var existing = await _reasonService.FindByText(cleanText);
            if (existing != null)
            {
                savedReason = existing;
                savedReason.Text = cleanText;
                await _reasonService.Update(savedReason);
            }
            else
            {
                savedReason = GetSelectedReason();
                await _reasonService.Add(savedReason);
            }
        }
        else
        {
            savedReason = SelectedReason;
            savedReason.Text = cleanText;
            await _reasonService.Update(savedReason);
        }

        await _reasonService.SubmitChanges();
        await LoadReasonsAsync();
        SelectedReason = FilteredReasons.FirstOrDefault(r => r.Id == savedReason.Id);
        
        return savedReason;
    }

    public async Task DeleteReasonAsync()
    {
        if (SelectedReason is null)
            return;

        var selected = SelectedReason;
        AppLogger.LogInfo($"Deleting Reason: Id={selected.Id}, Text={selected.Text}");

        await _reasonService.Delete(selected);
        await _reasonService.SubmitChanges();

        ClearSelectedReason();
        await LoadReasonsAsync();
        AppLogger.LogInfo($"Reason Id={selected.Id} deleted successfully.");
    }

    public void Dispose()
    {
    }
}
