using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Models.Generation;
using AMEFManager.Services;
using AMEFManager.ViewModels.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.Windows;

public partial class C802GenerationWindowViewModel : ViewModelBase
{
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;
    private readonly C802DocumentService _c802DocumentService;
    
    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    public C802GenerationWindowViewModel(
        AmefService amefService, 
        DocumentGenerationService documentGenerationService,
        SettingsService settingsService,
        C802DocumentService c802DocumentService)
    {
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
        _c802DocumentService = c802DocumentService;
        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;
    }

    private List<string> ValidateAmefs(IEnumerable<AMEFManager.Models.Amef> amefs)
    {
        var errors = new List<string>();
        if (!amefs.Any())
        {
            errors.Add("Cel putin un AMEF trebuie selectat.");
            return errors;
        }

        if (amefs.Any(a => string.IsNullOrWhiteSpace(a.NUI)))
        {
            errors.Add("Toate AMEF-urile selectate trebuie să aibă un NUI valid completat.");
        }

        if (amefs.Any(a => string.IsNullOrWhiteSpace(a.Series)))
        {
            errors.Add("Toate AMEF-urile selectate trebuie să aibă o serie validă.");
        }

        return errors;
    }

    [RelayCommand]
    private async Task GenerateC802Async()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[C802GenerationWindowViewModel.GenerateC802Async] Command execution started.");
        
        try
        {
            var amefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? new List<AMEFManager.Models.Amef>();
            AppLogger.LogDebug($"[C802GenerationWindowViewModel] Validating selection: {amefs.Count} AMEF item(s) selected.");

            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[C802GenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var settings = _settingsService.GetSettings();
            if (string.IsNullOrWhiteSpace(settings.Cui) || string.IsNullOrWhiteSpace(settings.NumeSocietate))
            {
                ErrorMessage = "Datele societății (CUI sau Denumire) nu sunt configurate în Setări.";
                AppLogger.LogWarning("[C802GenerationWindowViewModel] Company settings missing: CUI or NumeSocietate is empty.");
                return;
            }

            if (amefs.Any(a => string.IsNullOrWhiteSpace(a.NUI)))
            {
                ErrorMessage = "Toate AMEF-urile selectate trebuie să aibă un NUI valid completat.";
                AppLogger.LogWarning("[C802GenerationWindowViewModel] AMEF missing valid NUI.");
                return;
            }

            AppLogger.LogInfo($"[C802GenerationWindowViewModel] Selection validation passed for {amefs.Count} AMEF(s).");

            var nextId = await _c802DocumentService.GetNextNumberAsync();
            string cleanCif = (settings.Cui ?? string.Empty).Replace("RO", "", StringComparison.OrdinalIgnoreCase).Trim();

            var c802 = new C802Type
            {
                IdSolicitare = nextId,
                An = 2019,
                Luna = 12,
                TotalPlataA = amefs.Count,
                Cif = cleanCif,
                DenSolicitant = settings.NumeSocietate ?? string.Empty
            };

            AppLogger.LogInfo($"[C802GenerationWindowViewModel] C802 model populated: IdSolicitare={c802.IdSolicitare}, An={c802.An}, Luna={c802.Luna}, TotalPlataA={c802.TotalPlataA}, CIF='{c802.Cif}', DenSolicitant='{c802.DenSolicitant}'");

            foreach (var a in amefs)
            {
                AppLogger.LogDebug($"[C802GenerationWindowViewModel] Mapping AMEF item: NUI='{a.NUI}', Utilizare=1");
                c802.Amef.Add(new C802AMEF
                {
                    Nui = a.NUI ?? string.Empty,
                    Utilizare = 1
                });
            }

            string anafBase = !string.IsNullOrWhiteSpace(settings.ANAFDocumentsPath)
                ? settings.ANAFDocumentsPath
                : (!string.IsNullOrWhiteSpace(settings.ServerPath)
                    ? Path.Combine(settings.ServerPath, "ANAF")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ANAF"));
            var destinationDir = Path.Combine(anafBase, "C802", DateTime.Now.Month.ToString("D2"));
            Directory.CreateDirectory(destinationDir);
            AppLogger.LogDebug($"[C802GenerationWindowViewModel] Target output directory ensured: '{destinationDir}'");

            var invalidChars = Path.GetInvalidFileNameChars();
            var seriesString = string.Join("_", amefs.Select(a => string.Concat((a.Series ?? "Fara_Serie").Select(c => invalidChars.Contains(c) ? '_' : c)).Trim()));
            var xmlPath = Path.Combine(destinationDir, $"C802_{seriesString}.xml");

            if (File.Exists(xmlPath))
            {
                AppLogger.LogDebug($"[C802GenerationWindowViewModel] Overwriting existing XML data file at '{xmlPath}'");
                File.Delete(xmlPath);
            }

            AppLogger.LogDebug($"[C802GenerationWindowViewModel] Initializing XmlSerializer for C802Type with XmlQualifiedName.Empty namespaces...");
            var serializer = new XmlSerializer(typeof(C802Type));
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var writer = new StreamWriter(xmlPath))
            {
                serializer.Serialize(writer, c802, emptyNamespaces);
            }

            long xmlSize = new FileInfo(xmlPath).Length;
            AppLogger.LogInfo($"[C802GenerationWindowViewModel] XML serialization completed. Output file: '{xmlPath}', Size: {xmlSize} bytes.");

            var pdfPath = Path.Combine(destinationDir, $"C802_{seriesString}.pdf");

            string templatePdfPath = TemplatePathResolver.ResolveTemplatePath("C802_template.pdf", nameof(C802GenerationWindowViewModel));

            AppLogger.LogInfo($"[C802GenerationWindowViewModel] Resolved PDF template path: '{templatePdfPath}'");
            AppLogger.LogInfo($"[C802GenerationWindowViewModel] Invoking DocumentGenerationService.GeneratePdfAsync with XML: '{xmlPath}', Template: '{templatePdfPath}', Output: '{pdfPath}'");

            await _documentGenerationService.GeneratePdfAsync(xmlPath, templatePdfPath, pdfPath);

            AppLogger.LogDebug($"[C802GenerationWindowViewModel] Persisting C802Document record to database with Number={nextId}, AmefCount={amefs.Count}...");
            var doc = new C802Document { Number = nextId, Amefs = amefs.ToList() };
            await _c802DocumentService.Add(doc);
            await _c802DocumentService.SubmitChanges();
            AppLogger.LogInfo($"[C802GenerationWindowViewModel] C802Document record successfully persisted.");

            stopwatch.Stop();
            GenerationResult = "Generare cu succes!";
            AppLogger.LogInfo($"[C802GenerationWindowViewModel] C802 PDF generation completed successfully in {stopwatch.ElapsedMilliseconds}ms. Destination: '{pdfPath}'");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la generare: {ex.Message}";
            AppLogger.LogException(ex, "C802GenerationWindowViewModel.GenerateC802Async");
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
