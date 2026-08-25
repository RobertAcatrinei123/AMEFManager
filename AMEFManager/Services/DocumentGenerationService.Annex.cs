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
    public virtual Task<string> GenerateContractAnnexAsync(AdditionalDocument additionalDocument, string outputPath)
    {
        if (additionalDocument == null) throw new ArgumentNullException(nameof(additionalDocument));
        if (additionalDocument.Contract == null) throw new ArgumentNullException(nameof(additionalDocument.Contract));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

        string templatePath = TemplatePathResolver.ResolveTemplatePath("anexa_template.docx", nameof(DocumentGenerationService));
        AppLogger.LogInfo($"[DocumentGenerationService] Generating contract annex from template '{templatePath}' to '{outputPath}' for contract #{additionalDocument.Contract.Number}");

        EnsureDirectoryExists(outputPath);
        File.Copy(templatePath, outputPath, overwrite: true);

        var replacements = BuildContractAnnexReplacements(additionalDocument);

        using (var doc = WordprocessingDocument.Open(outputPath, true))
        {
            if (doc.MainDocumentPart != null)
            {
                if (doc.MainDocumentPart.Document?.Body != null)
                {
                    ReplacePlaceholdersAndTable(doc.MainDocumentPart.Document.Body, replacements, additionalDocument.Contract);
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

        AppLogger.LogInfo($"[DocumentGenerationService] Contract annex successfully generated at '{outputPath}'");
        return Task.FromResult(outputPath);
    }

    private static Dictionary<string, string> BuildContractAnnexReplacements(AdditionalDocument doc)
    {
        var contract = doc.Contract;
        var client = contract?.Client;
        var person = client?.Person;
        var address = client?.Address;
        var type = contract?.Type;

        string representative = person != null ? $"{person.LastName} {person.FirstName}".Trim() : string.Empty;
        string frequency = type != null ? type.GetFrequency() : "COMPLETEAZA MANUAL";
        string billingRules = type != null ? type.GetBillingRules() : string.Empty;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "{{nr}}", doc.Number.ToString() },
            { "{{date}}", doc.Date.ToString("dd.MM.yyyy") },
            { "{{contractNr}}", contract?.Number.ToString() ?? string.Empty },
            { "{{contractDate}}", contract?.Date.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{{clientName}}", client?.Name ?? string.Empty },
            { "{{clientAddress}}", address?.GetFullAddress() ?? string.Empty },
            { "{{regNr}}", client?.RegistrationNumber ?? string.Empty },
            { "{{cui}}", client?.GetFormattedCui() ?? string.Empty },
            { "{{reprezentant}}", representative },
            { "{{role}}", person?.Role ?? string.Empty },
            { "{{priceValue}}", type?.Value.ToString() ?? "0" },
            { "{{frequency}}", frequency },
            { "{{billingRules}}", billingRules }
        };
    }
}
