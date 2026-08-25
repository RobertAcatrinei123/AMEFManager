using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Models.Generation;
using AMEFManager.Services;
using AMEFManager.ViewModels.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.Windows;

public partial class F4103GenerationWindowViewModel : ViewModelBase
{
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;

    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    public F4103GenerationWindowViewModel(
        AmefService amefService,
        DocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;
    }

    private List<string> ValidateAmefs(IEnumerable<Amef> amefs)
    {
        var errors = new List<string>();
        if (!amefs.Any())
        {
            errors.Add("Cel putin un AMEF trebuie selectat.");
            return errors;
        }

        if (amefs.Any(a => a.Contract?.Client == null))
        {
            errors.Add("Toate AMEF-urile selectate trebuie să fie asociate unui client valid (prin contract).");
        }

        if (amefs.Any(a => string.IsNullOrWhiteSpace(a.NUI)))
        {
            errors.Add("Toate AMEF-urile selectate trebuie să aibă un NUI valid completat.");
        }

        if (amefs.Any(a => string.IsNullOrWhiteSpace(a.Series)))
        {
            errors.Add("Toate AMEF-urile selectate trebuie să aibă o serie validă.");
        }

        if (amefs.Any(a => a.Authorization == null))
        {
            errors.Add("Toate AMEF-urile selectate trebuie să aibă o autorizație asociată.");
        }

        return errors;
    }

    [RelayCommand]
    private async Task GenerateF4103Async()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[F4103GenerationWindowViewModel.GenerateF4103Async] Command execution started.");

        try
        {
            var amefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? new List<Amef>();
            AppLogger.LogDebug($"[F4103GenerationWindowViewModel] Validating selection: {amefs.Count} AMEF item(s) selected.");

            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[F4103GenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var settings = _settingsService.GetSettings();

            if (string.IsNullOrWhiteSpace(settings.Cui) || string.IsNullOrWhiteSpace(settings.NumeSocietate))
            {
                ErrorMessage = "Datele societății (CUI sau Denumire) nu sunt configurate în Setări.";
                AppLogger.LogWarning("[F4103GenerationWindowViewModel] Company settings missing: CUI or NumeSocietate is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.DenumireContabil) || string.IsNullOrWhiteSpace(settings.CnpContabil))
            {
                ErrorMessage = "Datele contabilului (Denumire sau CNP/CIF) nu sunt configurate în Setări.";
                AppLogger.LogWarning("[F4103GenerationWindowViewModel] Accountant settings missing: DenumireContabil or CnpContabil is empty.");
                return;
            }

            var invalidAmefs = amefs.Where(a => a.Contract?.Client == null || a.Authorization == null || string.IsNullOrWhiteSpace(a.NUI)).ToList();
            if (invalidAmefs.Any())
            {
                ErrorMessage = "Toate AMEF-urile selectate trebuie să aibă client, autorizație și NUI asociate.";
                AppLogger.LogWarning("[F4103GenerationWindowViewModel] Some selected AMEFs are missing client, authorization, or NUI.");
                return;
            }

            AppLogger.LogInfo($"[F4103GenerationWindowViewModel] Selection validation passed for {amefs.Count} AMEF(s).");

            string cleanCui = Regex.Replace(settings.Cui ?? "", @"[^\d]", "");
            long numericCui = long.TryParse(cleanCui, out long parsed) ? parsed : 0L;
            long totalPlataA = numericCui + amefs.Count;

            var f4103 = new F4103Type();
            f4103.Antet.NumeDoc.AnR = DateTime.Now.Year;
            f4103.Antet.NumeDoc.LunaR = DateTime.Now.Month;
            f4103.Antet.NumeDoc.TotalPlataA = totalPlataA;
            f4103.Antet.NumeDoc.UniversalCode = "F4103_A1.0.0";
            f4103.Antet.NumeDoc.DRec = 0;
            f4103.Antet.NumeDoc.DZero = 0;

            f4103.InfP.CalD1 = 0;
            f4103.InfP.CalD2 = 1;
            f4103.InfP.Cif = cleanCui;
            f4103.InfP.Den = settings.NumeSocietate ?? string.Empty;
            f4103.InfP.Email = settings.Email ?? string.Empty;
            f4103.InfP.Telefon = Regex.Replace(settings.Telefon ?? "", @"[^\d]", "");

            f4103.InfS.Sub2.CifP = settings.CnpContabil ?? string.Empty;
            f4103.InfS.Sub2.DenP = settings.DenumireContabil ?? string.Empty;
            f4103.InfS.Sub2.EmailP = settings.EmailContabil ?? string.Empty;
            f4103.InfS.Sub2.TelP = Regex.Replace(settings.TelContabil ?? "", @"[^\d]", "");
            f4103.InfS.Sub2.CalP1 = 1;
            f4103.InfS.Sub2.CalP2 = 0;

            AppLogger.LogInfo($"[F4103GenerationWindowViewModel] Header populated: An={f4103.Antet.NumeDoc.AnR}, Luna={f4103.Antet.NumeDoc.LunaR}, TotalPlata_A={f4103.Antet.NumeDoc.TotalPlataA}, CUI='{f4103.InfP.Cif}', Den='{f4103.InfP.Den}'");

            foreach (var a in amefs)
            {
                string cleanNui = Regex.Replace(a.NUI ?? "", @"[^\d]", "");

                var client = a.Contract?.Client;
                string cleanClientCui = client?.NationalIdentifier ?? string.Empty;
                string clientName = client?.Name ?? string.Empty;

                var item = new F4103AMEF();
                item.Sub2.Nui = cleanNui;
                item.Sub2.SeriaI = a.Series ?? string.Empty;
                item.Sub2.CifF = cleanClientCui;
                item.Sub2.DenF = clientName;
                item.Sub2.BifaL = 1;
                item.Sub2.NrAvizIci = a.Authorization?.Number.ToString() ?? string.Empty;
                item.Sub2.DataAvizIci = a.Authorization != null && a.Authorization.Date != default
                    ? a.Authorization.Date.ToString("dd.MM.yyyy")
                    : string.Empty;

                f4103.Amef.Add(item);
            }

            string anafBase = !string.IsNullOrWhiteSpace(settings.ANAFDocumentsPath)
                ? settings.ANAFDocumentsPath
                : (!string.IsNullOrWhiteSpace(settings.ServerPath)
                    ? Path.Combine(settings.ServerPath, "ANAF")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ANAF"));
            var destinationDir = Path.Combine(anafBase, "F4103", DateTime.Now.Month.ToString("D2"));
            Directory.CreateDirectory(destinationDir);
            AppLogger.LogDebug($"[F4103GenerationWindowViewModel] Target output directory ensured: '{destinationDir}'");

            var invalidChars = Path.GetInvalidFileNameChars();
            var seriesString = string.Join("_", amefs.Select(a => string.Concat((a.Series ?? "Fara_Serie").Select(c => invalidChars.Contains(c) ? '_' : c)).Trim()).Take(3));
            if (amefs.Count > 3) seriesString += $"_and_{amefs.Count - 3}_more";

            var xmlPath = Path.Combine(destinationDir, $"F4103_{seriesString}.xml");
            if (File.Exists(xmlPath))
            {
                AppLogger.LogDebug($"[F4103GenerationWindowViewModel] Overwriting existing XML data file at '{xmlPath}'");
                File.Delete(xmlPath);
            }

            var serializer = new XmlSerializer(typeof(F4103Type));
            var namespaces = new XmlSerializerNamespaces(new[] { System.Xml.XmlQualifiedName.Empty });

            using (var writer = new StreamWriter(xmlPath))
            {
                serializer.Serialize(writer, f4103, namespaces);
            }

            long xmlSize = new FileInfo(xmlPath).Length;
            AppLogger.LogInfo($"[F4103GenerationWindowViewModel] XML serialization completed. Output file: '{xmlPath}', Size: {xmlSize} bytes.");

            var pdfPath = Path.Combine(destinationDir, $"F4103_{seriesString}.pdf");
            string templatePdfPath = TemplatePathResolver.ResolveTemplatePath("F4103_template.pdf", nameof(F4103GenerationWindowViewModel));

            AppLogger.LogInfo($"[F4103GenerationWindowViewModel] Resolved PDF template path: '{templatePdfPath}'");

            await _documentGenerationService.GeneratePdfAsync(xmlPath, templatePdfPath, pdfPath);

            stopwatch.Stop();
            GenerationResult = "Generare cu succes!";
            AppLogger.LogInfo($"[F4103GenerationWindowViewModel] F4103 PDF generation completed successfully in {stopwatch.ElapsedMilliseconds}ms. Destination: '{pdfPath}'");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la generare: {ex.Message}";
            AppLogger.LogException(ex, "F4103GenerationWindowViewModel.GenerateF4103Async");
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
