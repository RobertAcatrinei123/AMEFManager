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
    public virtual Task<string> GenerateServiceContractAsync(Contract contract, string outputPath)
    {
        if (contract == null) throw new ArgumentNullException(nameof(contract));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

        string templatePath = TemplatePathResolver.ResolveTemplatePath("contract_template.docx", nameof(DocumentGenerationService));
        AppLogger.LogInfo($"[DocumentGenerationService] Generating service contract from template '{templatePath}' to '{outputPath}' for contract #{contract.Number}");

        EnsureDirectoryExists(outputPath);
        File.Copy(templatePath, outputPath, overwrite: true);

        var replacements = BuildServiceContractReplacements(contract);

        using (var doc = WordprocessingDocument.Open(outputPath, true))
        {
            if (doc.MainDocumentPart != null)
            {
                // 1. Process Main Document Body
                if (doc.MainDocumentPart.Document?.Body != null)
                {
                    ReplacePlaceholdersAndTable(doc.MainDocumentPart.Document.Body, replacements, contract);
                }

                // 2. Process Headers
                foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                    {
                        ReplacePlaceholdersInElement(headerPart.Header, replacements);
                        headerPart.Header.Save();
                    }
                }

                // 3. Process Footers
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

        AppLogger.LogInfo($"[DocumentGenerationService] Service contract successfully generated at '{outputPath}'");
        return Task.FromResult(outputPath);
    }

    public virtual Task<string> GeneratePaperContractAsync(Contract contract, string outputPath)
    {
        if (contract == null) throw new ArgumentNullException(nameof(contract));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

        string templatePath = TemplatePathResolver.ResolveTemplatePath("contract_role_template.docx", nameof(DocumentGenerationService));
        AppLogger.LogInfo($"[DocumentGenerationService] Generating paper contract from template '{templatePath}' to '{outputPath}' for contract #{contract.Number}");

        EnsureDirectoryExists(outputPath);
        File.Copy(templatePath, outputPath, overwrite: true);

        var replacements = BuildPaperContractReplacements(contract);

        using (var doc = WordprocessingDocument.Open(outputPath, true))
        {
            if (doc.MainDocumentPart != null)
            {
                // 1. Process Main Document Body
                if (doc.MainDocumentPart.Document?.Body != null)
                {
                    ReplacePlaceholdersInElement(doc.MainDocumentPart.Document.Body, replacements);
                }

                // 2. Process Headers
                foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                    {
                        ReplacePlaceholdersInElement(headerPart.Header, replacements);
                        headerPart.Header.Save();
                    }
                }

                // 3. Process Footers
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

        AppLogger.LogInfo($"[DocumentGenerationService] Paper contract successfully generated at '{outputPath}'");
        return Task.FromResult(outputPath);
    }

    private static Dictionary<string, string> BuildServiceContractReplacements(Contract contract)
    {
        var client = contract.Client;
        var person = client?.Person;
        var address = client?.Address;
        var type = contract.Type;

        string representative = person != null ? $"{person.LastName} {person.FirstName}".Trim() : string.Empty;
        string frequency = type != null ? type.GetFrequency() : "COMPLETEAZA MANUAL";
        string billingRules = type != null ? type.GetBillingRules() : string.Empty;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "{{nr}}", contract.Number.ToString() },
            { "{{date}}", contract.Date.ToString("dd.MM.yyyy") },
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

    private static Dictionary<string, string> BuildPaperContractReplacements(Contract contract)
    {
        var client = contract.Client;
        var person = client?.Person;
        var address = client?.Address;
        var type = contract.Type;

        string representative = person != null ? $"{person.LastName} {person.FirstName}".Trim() : string.Empty;
        string billingRules = type != null ? type.GetBillingRules() : string.Empty;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "{{nr}}", contract.Number.ToString() },
            { "{{date}}", contract.Date.ToString("dd.MM.yyyy") },
            { "{{clientName}}", client?.Name ?? string.Empty },
            { "{{clientAddress}}", address?.GetFullAddress() ?? string.Empty },
            { "{{regNr}}", client?.RegistrationNumber ?? string.Empty },
            { "{{cui}}", client?.GetFormattedCui() ?? string.Empty },
            { "{{reprezentant}}", representative },
            { "{{role}}", person?.Role ?? string.Empty },
            { "{{billingRules}}", billingRules }
        };
    }
}
