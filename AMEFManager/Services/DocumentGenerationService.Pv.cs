using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AMEFManager.Helpers;
using AMEFManager.Models;
using DocumentFormat.OpenXml.Packaging;

namespace AMEFManager.Services;

public partial class DocumentGenerationService
{
    public virtual Task<string> GenerateSealingDocumentAsync(Amef amef, string outputPath, int number, DateOnly? date = null)
    {
        if (amef == null) throw new ArgumentNullException(nameof(amef));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

        string templatePath = TemplatePathResolver.ResolveTemplatePath("PV_sigilare_template.docx", nameof(DocumentGenerationService));
        AppLogger.LogInfo($"[DocumentGenerationService] Generating sealing document from template '{templatePath}' to '{outputPath}' for AMEF {amef.Series} (Nr={number})");

        EnsureDirectoryExists(outputPath);
        File.Copy(templatePath, outputPath, overwrite: true);

        var docDate = date ?? DateOnly.FromDateTime(DateTime.Now);
        var replacements = BuildSealingDocumentReplacements(amef, number, docDate);

        using (var doc = WordprocessingDocument.Open(outputPath, true))
        {
            if (doc.MainDocumentPart != null)
            {
                if (doc.MainDocumentPart.Document?.Body != null)
                {
                    ReplacePlaceholdersInElement(doc.MainDocumentPart.Document.Body, replacements);
                }

                foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                    {
                        ReplacePlaceholdersInElement(headerPart.Header, replacements);
                        headerPart.Header.Save();
                    }
                }

                foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                {
                    if (footerPart.Footer != null)
                    {
                        ReplacePlaceholdersInElement(footerPart.Footer, replacements);
                        footerPart.Footer.Save();
                    }
                }

                doc.MainDocumentPart.Document?.Save();
            }
        }

        AppLogger.LogInfo($"[DocumentGenerationService] Sealing document successfully generated at '{outputPath}'");
        return Task.FromResult(outputPath);
    }

    private static Dictionary<string, string> BuildSealingDocumentReplacements(Amef amef, int number, DateOnly date)
    {
        var contract = amef.Contract;
        var client = contract?.Client;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "{{nr}}", number.ToString() },
            { "{{date}}", date.ToString("dd.MM.yyyy") },
            { "{{brand}}", amef.GetBrand() },
            { "{{model}}", amef.Model ?? string.Empty },
            { "{{authNr}}", amef.Authorization?.Number.ToString() ?? string.Empty },
            { "{{authDate}}", amef.Authorization?.Date.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{{series}}", amef.Series ?? string.Empty },
            { "{{clientName}}", client?.Name ?? string.Empty },
            { "{{cui}}", client?.GetFormattedCui() ?? string.Empty },
            { "{{amefAddress}}", amef.Address?.GetFullAddress() ?? client?.Address?.GetFullAddress() ?? string.Empty }
        };
    }

    public virtual Task<string> GenerateDeliveryDocumentAsync(Amef amef, string outputPath, int number, string? reason = null, DateOnly? date = null)
    {
        if (amef == null) throw new ArgumentNullException(nameof(amef));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

        string templatePath = TemplatePathResolver.ResolveTemplatePath("PV_predare_template.docx", nameof(DocumentGenerationService));
        AppLogger.LogInfo($"[DocumentGenerationService] Generating delivery document from template '{templatePath}' to '{outputPath}' for AMEF {amef.Series} (Nr={number}, Reason='{reason}')");

        EnsureDirectoryExists(outputPath);
        File.Copy(templatePath, outputPath, overwrite: true);

        var docDate = date ?? DateOnly.FromDateTime(DateTime.Now);
        var replacements = BuildDeliveryDocumentReplacements(amef, number, reason, docDate);

        using (var doc = WordprocessingDocument.Open(outputPath, true))
        {
            if (doc.MainDocumentPart != null)
            {
                if (doc.MainDocumentPart.Document?.Body != null)
                {
                    ReplacePlaceholdersInElement(doc.MainDocumentPart.Document.Body, replacements);
                }

                foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                    {
                        ReplacePlaceholdersInElement(headerPart.Header, replacements);
                        headerPart.Header.Save();
                    }
                }

                foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                {
                    if (footerPart.Footer != null)
                    {
                        ReplacePlaceholdersInElement(footerPart.Footer, replacements);
                        footerPart.Footer.Save();
                    }
                }

                doc.MainDocumentPart.Document?.Save();
            }
        }

        AppLogger.LogInfo($"[DocumentGenerationService] Delivery document successfully generated at '{outputPath}'");
        return Task.FromResult(outputPath);
    }

    private static Dictionary<string, string> BuildDeliveryDocumentReplacements(Amef amef, int number, string? reason, DateOnly date)
    {
        var contract = amef.Contract;
        var client = contract?.Client;
        var person = client?.Person;
        var clientAddress = client?.Address;

        string representative = person != null ? $"{person.LastName} {person.FirstName}".Trim() : string.Empty;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "{{nr}}", number.ToString() },
            { "{{date}}", date.ToString("dd.MM.yyyy") },
            { "{{clientName}}", client?.Name ?? string.Empty },
            { "{{reprezentantName}}", representative },
            { "{{reprezentant}}", representative },
            { "{{role}}", person?.Role ?? string.Empty },
            { "{{clientAddress}}", clientAddress?.GetFullAddress() ?? string.Empty },
            { "{{clientCui}}", client?.GetFormattedCui() ?? string.Empty },
            { "{{amefType}}", amef.GetDeviceType() },
            { "{{model}}", amef.Model ?? string.Empty },
            { "{{authNr}}", amef.Authorization?.Number.ToString() ?? string.Empty },
            { "{{authDate}}", amef.Authorization?.Date.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{{amefSeries}}", amef.Series ?? string.Empty },
            { "{{amefNUI}}", amef.NUI ?? string.Empty },
            { "{{amefAddress}}", amef.Address?.GetFullAddress() ?? clientAddress?.GetFullAddress() ?? string.Empty },
            { "{{reason}}", reason ?? string.Empty }
        };
    }
}
