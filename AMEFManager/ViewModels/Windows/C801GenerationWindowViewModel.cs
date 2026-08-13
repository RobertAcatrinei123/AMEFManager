using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AMEFManager.Helpers;
using AMEFManager.Models.Generation;
using AMEFManager.Services;
using AMEFManager.ViewModels.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMEFManager.ViewModels.Windows;

public partial class C801GenerationWindowViewModel : ViewModelBase
{
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly SettingsService _settingsService;
    
    public AmefMultiSelectUserControlViewModel AmefMultiSelectUserControlViewModel { get; }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _generationResult;
    [ObservableProperty] private string? _errorMessage;

    public C801GenerationWindowViewModel(
        AmefService amefService, 
        DocumentGenerationService documentGenerationService,
        SettingsService settingsService)
    {
        _documentGenerationService = documentGenerationService;
        _settingsService = settingsService;
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

        var clientIds = amefs.Select(a => a.Contract?.ClientId).Distinct().ToList();
        if (clientIds.Count > 1)
        {
            errors.Add("Toate AMEF-urile selectate trebuie sa aiba acelasi Client.");
        }
        else if (clientIds.Count == 1 && clientIds[0] == null)
        {
            errors.Add("AMEF-urile selectate trebuie sa fie asociate unui Client valid (prin contract).");
        }

        return errors;
    }

    [RelayCommand]
    private async Task GenerateC801Async()
    {
        IsGenerating = true;
        ErrorMessage = null;
        GenerationResult = null;

        var stopwatch = Stopwatch.StartNew();
        AppLogger.LogInfo("[C801GenerationWindowViewModel.GenerateC801Async] Command execution started.");
        
        try
        {
            var amefs = AmefMultiSelectUserControlViewModel.SelectedAmefs?.ToList() ?? new List<AMEFManager.Models.Amef>();
            AppLogger.LogDebug($"[C801GenerationWindowViewModel] Validating selection: {amefs.Count} AMEF item(s) selected.");

            var validationErrors = AmefMultiSelectUserControlViewModel.Validate();
            if (validationErrors.Any())
            {
                string errorSummary = string.Join("; ", validationErrors);
                AppLogger.LogWarning($"[C801GenerationWindowViewModel] Selection validation failed: {errorSummary}");
                ErrorMessage = string.Join("\n", validationErrors);
                return;
            }

            AppLogger.LogInfo($"[C801GenerationWindowViewModel] Selection validation passed for {amefs.Count} AMEF(s).");

            var firstAmef = amefs.FirstOrDefault();
            var client = firstAmef?.Contract?.Client;
            var person = client?.Person;

            if (client == null || person == null)
            {
                var invEx = new InvalidOperationException("Selected AMEF does not have a valid Client or Representative Person associated.");
                AppLogger.LogError("[C801GenerationWindowViewModel] Client or Person entity missing.", invEx);
                throw invEx;
            }

            string clientCif = client.NationalIdentifier ?? string.Empty;
            string clientAddress = client.Address?.GetFullAddress() ?? string.Empty;
            string repAddress = person.Address?.GetFullAddress() ?? string.Empty;
            string repName = $"{person.LastName} {person.FirstName}".Trim();
            string repCiDate = person.IssuingDate != default ? person.IssuingDate.ToString("dd.MM.yyyy") : string.Empty;

            var c801 = new C801Type
            {
                AnR = DateTime.Now.Year,
                LunaR = DateTime.Now.Month,
                TotalPlataA = amefs.Count,
                Cif = clientCif,
                DenSoc = client.Name ?? string.Empty,
                AdresaSoc = clientAddress,
                CifRep = person.Cnp ?? string.Empty,
                NumeRep = repName,
                AdresaRep = repAddress,
                Tip1 = 1,
                Tip2 = 0,
                DataCiRep = repCiDate,
                SerieCiRep = person.Series ?? string.Empty,
                NrCiRep = person.Number ?? string.Empty,
                EliberatCiRep = person.Issuer ?? string.Empty,
                CalitateRep = person.Role ?? string.Empty
            };

            AppLogger.LogInfo($"[C801GenerationWindowViewModel] C801 header populated: AnR={c801.AnR}, LunaR={c801.LunaR}, TotalPlataA={c801.TotalPlataA}, CIF='{c801.Cif}', DenSoc='{c801.DenSoc}', CifRep='{c801.CifRep}', NumeRep='{c801.NumeRep}', CalitateRep='{c801.CalitateRep}', Tip1={c801.Tip1}, Tip2={c801.Tip2}");

            foreach (var a in amefs)
            {
                string devType = a.GetDeviceType();
                string devConfig = a.GetConfig();
                string authNum = a.Authorization?.Number.ToString() ?? string.Empty;
                string authDate = a.Authorization != null && a.Authorization.Date != default ? a.Authorization.Date.ToString("dd.MM.yyyy") : string.Empty;
                string city = a.Address?.City ?? "Nu e specificat";
                string street = a.Address?.Street ?? "Nu e specificat";
                string restAdr = a.Address?.GetRestAddress() ?? string.Empty;
                int countyCode = CountyHelper.GetCountyCode(a.Address?.County ?? client.Address?.County);

                AppLogger.LogDebug($"[C801GenerationWindowViewModel] Mapping AMEF: Serie='{a.Series}', Model='{a.Model}', Tip='{devType}', Config='{devConfig}', NrAutoriz='{authNum}', DataAutoriz='{authDate}', Jud={countyCode}, Loc='{city}', Strada='{street}', RestAdr='{restAdr}', TipActivitate=3");

                c801.AMEF.Add(new AMEF
                {
                    Sub2 = new AmefSub2
                    {
                        Tip = devType,
                        Model = a.Model ?? string.Empty,
                        Config = devConfig,
                        Serie = a.Series ?? string.Empty,
                        NrAutoriz = authNum,
                        DataAutoriz = authDate,
                        Jud = countyCode,
                        Loc = city,
                        Strada = street,
                        RestAdr = restAdr,
                        TipActivitate = 3,
                    }
                });
            }

            var settings = _settingsService.GetSettings();
            string outputBaseDir = !string.IsNullOrWhiteSpace(settings.ClientPath)
                ? settings.ClientPath
                : Path.Combine(settings.ServerPath, "Contracte Clientii");

            var contract = firstAmef?.Contract;
            var invalidChars = Path.GetInvalidFileNameChars();
            string clientName = client?.Name ?? $"Client_{contract?.Number ?? 0}";
            string sanitizedClientName = string.Concat(clientName.Select(c => invalidChars.Contains(c) ? '_' : c)).Trim();
            if (string.IsNullOrWhiteSpace(sanitizedClientName))
            {
                sanitizedClientName = $"Client_{contract?.Number ?? 0}";
            }

            string contractNum = contract != null ? contract.Number.ToString("D4") : "0000";
            string clientFolder = $"{contractNum} - {sanitizedClientName}";
            var seriesString = string.Join("_", amefs.Select(a => string.Concat((a.Series ?? "Fara_Serie").Select(c => invalidChars.Contains(c) ? '_' : c)).Trim()));

            var destinationDir = Path.Combine(outputBaseDir, clientFolder, seriesString, "Atribuire NUI");
            Directory.CreateDirectory(destinationDir);
            AppLogger.LogDebug($"[C801GenerationWindowViewModel] Target output directory ensured: '{destinationDir}'");

            var xmlPath = Path.Combine(destinationDir, $"C801_{seriesString}.xml");

            if (File.Exists(xmlPath))
            {
                AppLogger.LogDebug($"[C801GenerationWindowViewModel] Overwriting existing XML data file at '{xmlPath}'");
                File.Delete(xmlPath);
            }

            AppLogger.LogDebug($"[C801GenerationWindowViewModel] Initializing XmlSerializer for C801Type with XmlQualifiedName.Empty namespaces...");
            var serializer = new XmlSerializer(typeof(C801Type));
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var writer = new StreamWriter(xmlPath))
            {
                serializer.Serialize(writer, c801, emptyNamespaces);
            }

            long xmlSize = new FileInfo(xmlPath).Length;
            AppLogger.LogInfo($"[C801GenerationWindowViewModel] XML serialization completed. Output file: '{xmlPath}', Size: {xmlSize} bytes.");

            var pdfPath = Path.Combine(destinationDir, $"C801_{seriesString}.pdf");

            string templatePdfPath = TemplatePathResolver.ResolveTemplatePath("C801_template.pdf", nameof(C801GenerationWindowViewModel));

            AppLogger.LogInfo($"[C801GenerationWindowViewModel] Resolved PDF template path: '{templatePdfPath}'");
            AppLogger.LogInfo($"[C801GenerationWindowViewModel] Invoking DocumentGenerationService.GeneratePdfAsync with XML: '{xmlPath}', Template: '{templatePdfPath}', Output: '{pdfPath}'");

            await _documentGenerationService.GeneratePdfAsync(xmlPath, templatePdfPath, pdfPath);

            stopwatch.Stop();
            GenerationResult = "Generare cu succes!";
            AppLogger.LogInfo($"[C801GenerationWindowViewModel] C801 PDF generation completed successfully in {stopwatch.ElapsedMilliseconds}ms. Destination: '{pdfPath}'");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la generare: {ex.Message}";
            AppLogger.LogException(ex, "C801GenerationWindowViewModel.GenerateC801Async");
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
