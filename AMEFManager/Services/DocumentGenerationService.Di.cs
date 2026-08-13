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
    public virtual Task<string> GenerateInstallationDeclarationAsync(Amef amef, string outputPath, bool isRefiscalizare = false, string? reason = null)
    {
        if (amef == null) throw new ArgumentNullException(nameof(amef));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

        string templatePath = TemplatePathResolver.ResolveTemplatePath("di_template.docx", nameof(DocumentGenerationService));
        AppLogger.LogInfo($"[DocumentGenerationService] Generating installation declaration from template '{templatePath}' to '{outputPath}' for AMEF {amef.Series} (Refiscalizare={isRefiscalizare}, Reason='{reason}')");

        EnsureDirectoryExists(outputPath);
        File.Copy(templatePath, outputPath, overwrite: true);

        var replacements = BuildInstallationDeclarationReplacements(amef, isRefiscalizare, reason);

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

        AppLogger.LogInfo($"[DocumentGenerationService] Installation declaration successfully generated at '{outputPath}'");
        return Task.FromResult(outputPath);
    }

    private static Dictionary<string, string> BuildInstallationDeclarationReplacements(Amef amef, bool isRefiscalizare = false, string? reason = null)
    {
        var contract = amef.Contract;
        var client = contract?.Client;
        var person = client?.Person;
        var address = client?.Address;

        string representative = person != null ? $"{person.LastName} {person.FirstName}".Trim() : string.Empty;

        string typeValue = isRefiscalizare ? "REFISCALIZARE" : string.Empty;
        string reasonValue = isRefiscalizare ? (reason ?? string.Empty) : string.Empty;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "{{reprezentant}}", representative },
            { "{{identityCardSeries}}", person?.Series ?? string.Empty },
            { "{{identityCardNr}}", person?.Number ?? string.Empty },
            { "{{issuer}}", person?.Issuer ?? string.Empty },
            { "{{issueDate}}", person?.IssuingDate.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{{role}}", person?.Role ?? string.Empty },
            { "{{clientName}}", client?.Name ?? string.Empty },
            { "{{address}}", address?.GetFullAddress() ?? string.Empty },
            { "{{cui}}", client?.GetFormattedCui() ?? string.Empty },
            { "{{fiscalizationDate}}", amef.FiscalizationDate.ToString("dd.MM.yyyy") },
            { "{{amefAddress}}", amef.Address?.GetFullAddress() ?? string.Empty },
            { "{{amefType}}", amef.GetDeviceType() },
            { "{{amefModel}}", amef.Model ?? string.Empty },
            { "{{amefSeries}}", amef.Series ?? string.Empty },
            { "{{authorizationNr}}", amef.Authorization?.Number.ToString() ?? string.Empty },
            { "{{authorizationDate}}", amef.Authorization?.Date.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{{amefNui}}", amef.NUI ?? string.Empty },
            { "{{billSeries}}", amef.Bill?.BillSeries ?? string.Empty },
            { "{{billNumber}}", amef.Bill?.BillNumber.ToString() ?? string.Empty },
            { "{{billDate}}", amef.Bill?.BillDate.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{{fiscalCity}}", amef.FiscalCity ?? string.Empty },
            { "{{type}}", typeValue },
            { "{{reason}}", reasonValue }
        };
    }
}
