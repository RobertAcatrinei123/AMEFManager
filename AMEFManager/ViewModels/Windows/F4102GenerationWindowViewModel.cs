using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AMEFManager.Helpers;
using AMEFManager.Models;
using AMEFManager.Models.Generation;
using AMEFManager.Services;
using AMEFManager.ViewModels.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.Windows;

public partial class F4102GenerationWindowViewModel : ViewModelBase
{
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;
    private AppSettings _appSettings;
    
    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;
    
    public F4102GenerationWindowViewModel(
        AmefService amefService, 
        DocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
        AmefMultiSelectUserControlViewModel = new AmefMultiSelectUserControlViewModel(amefService);
        AmefMultiSelectUserControlViewModel.Validator = ValidateAmefs;

        _appSettings = _settingsService.GetSettings();
    }

    private List<string> ValidateAmefs(IEnumerable<AMEFManager.Models.Amef> amefs)
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

        return errors;
    }

    [RelayCommand]
    private async Task GenerateF4102Async()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[F4102GenerationWindowViewModel.GenerateF4102Async] Command execution started.");
        
        try
        {
            var amefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? new List<AMEFManager.Models.Amef>();
            AppLogger.LogDebug($"[F4102GenerationWindowViewModel] Validating selection: {amefs.Count} AMEF item(s) selected.");

            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[F4102GenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            var settings = _settingsService.GetSettings();

            if (string.IsNullOrWhiteSpace(settings.Cui) || string.IsNullOrWhiteSpace(settings.NumeSocietate))
            {
                ErrorMessage = "Datele societății (CUI sau Denumire) nu sunt configurate în Setări.";
                AppLogger.LogWarning("[F4102GenerationWindowViewModel] Company settings missing: CUI or NumeSocietate is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.DenumireContabil) || string.IsNullOrWhiteSpace(settings.CnpContabil))
            {
                ErrorMessage = "Datele contabilului (Denumire sau CNP/CIF) nu sunt configurate în Setări.";
                AppLogger.LogWarning("[F4102GenerationWindowViewModel] Accountant settings missing: DenumireContabil or CnpContabil is empty.");
                return;
            }

            var invalidAmefs = amefs.Where(a => a.Contract?.Client == null).ToList();
            if (invalidAmefs.Any())
            {
                ErrorMessage = "Toate AMEF-urile selectate trebuie să aibă un contract și un client valid asociat.";
                AppLogger.LogWarning("[F4102GenerationWindowViewModel] Some selected AMEFs are missing contract or client.");
                return;
            }

            AppLogger.LogInfo($"[F4102GenerationWindowViewModel] Selection validation passed for {amefs.Count} AMEF(s).");
            
            var clientGroups = amefs.GroupBy(a => a.Contract!.Client!).ToList();
            AppLogger.LogInfo($"[F4102GenerationWindowViewModel] Client grouping: found {clientGroups.Count} distinct client group(s) across {amefs.Count} selected AMEF(s).");

            string rawCifSource = settings.Cui ?? string.Empty;
            string resolvedCompanyCif = rawCifSource.Replace("RO", "", StringComparison.OrdinalIgnoreCase).Trim();
            string resolvedCompanyName = settings.NumeSocietate ?? string.Empty;

            string cleanCui = System.Text.RegularExpressions.Regex.Replace(rawCifSource, @"[^\d]", "");
            long numericCif = long.TryParse(cleanCui, out long parsed) ? parsed : 0L;
            
            long totalPlataA = numericCif + clientGroups.Count;

            var f4102 = new F4102Type();
            
            f4102.Antet.IdDoc.UniversalCode = "F4102_A1.0.7";
            f4102.Antet.IdDoc.FormValid = "FORMULAR NEVALIDAT";
            f4102.Antet.NumeDoc.Header.InitMsg = "0";
            f4102.Antet.NumeDoc.TotalPlataA = totalPlataA;
            f4102.Antet.NumeDoc.DRec = 1;
            f4102.Antet.NumeDoc.AnR = DateTime.Now.Year;
            f4102.Antet.NumeDoc.LunaR = DateTime.Now.Month;

            f4102.CntFrm.Cif = !string.IsNullOrEmpty(cleanCui) ? cleanCui : resolvedCompanyCif;
            f4102.CntFrm.DenDS = resolvedCompanyName;
            f4102.CntFrm.CheckD = 0;
            f4102.CntFrm.CheckS = 1;
            f4102.CntFrm.EmailDS = settings.Email ?? string.Empty;
            f4102.CntFrm.TelefonDS = settings.Telefon ?? string.Empty;
            f4102.CntFrm.PrsInrg.CltP = 1;
            f4102.CntFrm.PrsInrg.RB = 1;
            f4102.CntFrm.PrsInrg.DenPI = settings.DenumireContabil ?? string.Empty;
            f4102.CntFrm.PrsInrg.CifPI = settings.CnpContabil ?? string.Empty;
            f4102.CntFrm.PrsInrg.EmailPI = settings.EmailContabil ?? string.Empty;
            f4102.CntFrm.PrsInrg.TelefonPI = settings.TelContabil ?? string.Empty;

            AppLogger.LogInfo($"[F4102GenerationWindowViewModel] Header metadata populated: Luna_r={f4102.Antet.NumeDoc.LunaR}, An_r={f4102.Antet.NumeDoc.AnR}, TotalPlata_A={f4102.Antet.NumeDoc.TotalPlataA} (CIF={numericCif} + Groups={clientGroups.Count}), CIF='{f4102.CntFrm.Cif}', DenDS='{f4102.CntFrm.DenDS}', CheckD={f4102.CntFrm.CheckD}, CheckS={f4102.CntFrm.CheckS}");

            int clientIndex = 1;
            foreach (var clientGroup in clientGroups)
            {
                var client = clientGroup.Key;
                string cleanClientCui = client.NationalIdentifier ?? string.Empty;

                AppLogger.LogDebug($"[F4102GenerationWindowViewModel] Group [{clientIndex}] -> Client Name: '{client.Name}', CUI/CIF: '{cleanClientCui}', AMEF count: {clientGroup.Count()}");

                var amefUtl = new AmefUtlF4102
                {
                    NU = clientIndex++,
                    CifU = cleanClientCui,
                    DenU = client.Name ?? string.Empty,
                    CntU = string.Empty,
                    Data1 = string.Empty,
                    Data2 = string.Empty
                };

                int amefIndex = 1;
                foreach (var a in clientGroup)
                {
                    string formattedDate = a.FiscalizationDate != default ? a.FiscalizationDate.ToString("dd.MM.yyyy") : string.Empty;
                    string city = a.Address?.City ?? string.Empty;
                    string street = a.Address?.Street ?? string.Empty;
                    string streetNr = a.Address?.StreetNumber ?? string.Empty;
                    string block = a.Address?.Block ?? string.Empty;
                    string floor = a.Address?.Floor ?? string.Empty;
                    string apt = a.Address?.Apartment ?? string.Empty;
                    string alt = !string.IsNullOrWhiteSpace(a.Address?.Other) 
                        ? a.Address.Other 
                        : (a.Address?.GetRestAddress() ?? string.Empty);
                    int countyCode = CountyHelper.GetCountyCode(a.Address?.County ?? client.Address?.County);

                    AppLogger.LogDebug($"[F4102GenerationWindowViewModel]  - Mapping AMEF [{amefIndex}]: nrAmef(NUI)='{a.NUI}', nrTs(Series)='{a.Series}', dataInsAmef='{formattedDate}', judet={countyCode}, loc='{city}', str='{street}', nr='{streetNr}', bloc='{block}', etaj='{floor}', apt='{apt}', alt='{alt}'");

                    var amefItem = new AmefItemF4102
                    {
                        NA = amefIndex++,
                        NrTs = a.Series ?? string.Empty,
                        NrAmef = a.NUI ?? string.Empty,
                        DataInsAmef = formattedDate,
                        LocInst = new LocInstF4102
                        {
                            Adr = new AdrF4102
                            {
                                Judet = countyCode,
                                Loc = city,
                                Str = street,
                                Nr = streetNr,
                                Bloc = block,
                                Etaj = floor,
                                Apt = apt,
                                Alt = alt
                            },
                            Amb = 0,
                            Nesupravegheat = 0
                        }
                    };

                    amefUtl.Amef.Add(amefItem);
                }

                f4102.AmefUtl.Add(amefUtl);
            }

            string anafBase = !string.IsNullOrWhiteSpace(settings.ANAFDocumentsPath)
                ? settings.ANAFDocumentsPath
                : (!string.IsNullOrWhiteSpace(settings.ServerPath)
                    ? Path.Combine(settings.ServerPath, "ANAF")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ANAF"));
            var destinationDir = Path.Combine(anafBase, "F4102", DateTime.Now.Month.ToString("D2"));
            Directory.CreateDirectory(destinationDir);
            AppLogger.LogDebug($"[F4102GenerationWindowViewModel] Target output directory ensured: '{destinationDir}'");

            var invalidChars = Path.GetInvalidFileNameChars();
            var seriesString = string.Join("_", amefs.Select(a => string.Concat((a.Series ?? "Fara_Serie").Select(c => invalidChars.Contains(c) ? '_' : c)).Trim()).Take(3));
            if (amefs.Count > 3) seriesString += $"_and_{amefs.Count - 3}_more";

            var xmlPath = Path.Combine(destinationDir, $"F4102_{seriesString}.xml");
            if (File.Exists(xmlPath))
            {
                AppLogger.LogDebug($"[F4102GenerationWindowViewModel] Overwriting existing XML data file at '{xmlPath}'");
                File.Delete(xmlPath);
            }

            AppLogger.LogDebug($"[F4102GenerationWindowViewModel] Initializing XmlSerializer for F4102Type with XmlQualifiedName.Empty namespaces...");
            var serializer = new XmlSerializer(typeof(F4102Type));
            var namespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });

            using (var writer = new StreamWriter(xmlPath))
            {
                serializer.Serialize(writer, f4102, namespaces);
            }

            long xmlSize = new FileInfo(xmlPath).Length;
            AppLogger.LogInfo($"[F4102GenerationWindowViewModel] XML serialization completed. Output file: '{xmlPath}', Size: {xmlSize} bytes.");

            var pdfPath = Path.Combine(destinationDir, $"F4102_{seriesString}.pdf");

            string templatePdfPath = TemplatePathResolver.ResolveTemplatePath("F4102_template.pdf", nameof(F4102GenerationWindowViewModel));

            AppLogger.LogInfo($"[F4102GenerationWindowViewModel] Resolved PDF template path: '{templatePdfPath}'");
            AppLogger.LogInfo($"[F4102GenerationWindowViewModel] Invoking DocumentGenerationService.GeneratePdfAsync with XML: '{xmlPath}', Template: '{templatePdfPath}', Output: '{pdfPath}'");

            await _documentGenerationService.GeneratePdfAsync(xmlPath, templatePdfPath, pdfPath);

            stopwatch.Stop();
            GenerationResult = "Generare cu succes!";
            AppLogger.LogInfo($"[F4102GenerationWindowViewModel] F4102 PDF generation completed successfully in {stopwatch.ElapsedMilliseconds}ms. Destination: '{pdfPath}'");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la generare: {ex.Message}";
            AppLogger.LogException(ex, "F4102GenerationWindowViewModel.GenerateF4102Async");
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
