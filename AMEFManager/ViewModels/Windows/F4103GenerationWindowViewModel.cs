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
            f4103.InfP.Den = settings.NumeSocietate;
            f4103.InfP.Email = settings.Email;
            f4103.InfP.Telefon = Regex.Replace(settings.Telefon ?? "", @"[^\d]", "");

            f4103.InfS.Sub2.CifP = settings.CnpContabil;
            f4103.InfS.Sub2.DenP = settings.DenumireContabil;
            f4103.InfS.Sub2.EmailP = settings.EmailContabil;
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
                item.Sub2.DataAvizIci = a.Authorization?.Date.ToString("dd.MM.yyyy") ?? string.Empty;

                f4103.Amef.Add(item);
            }

            var destinationDir = Path.Combine(settings.ANAFDocumentsPath, "F4103", DateTime.Now.Month.ToString("D2"));
            Directory.CreateDirectory(destinationDir);
            AppLogger.LogDebug($"[F4103GenerationWindowViewModel] Target output directory ensured: '{destinationDir}'");

            var seriesString = string.Join("_", amefs.Select(a => a.Series).Take(3));
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
