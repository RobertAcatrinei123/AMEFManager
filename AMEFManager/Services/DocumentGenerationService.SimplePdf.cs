using System;
using System.IO;
using System.Threading.Tasks;
using AMEFManager.Helpers;
using AMEFManager.Models;
using iTextSharp.text.pdf;

namespace AMEFManager.Services;

public partial class DocumentGenerationService : IDocumentGenerationService
{
    public virtual Task<string> CopyAuthorizationAsync(Amef amef, string outputPath)
    {
        if (amef == null) throw new ArgumentNullException(nameof(amef));
        if (amef.Authorization == null) throw new ArgumentException("AMEF has no associated Authorization.", nameof(amef));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));

        string templateFileName = Path.Combine("Authorizations", $"Autorizatia nr. {amef.Authorization.Number} din data de {amef.Authorization.Date:dd.MM.yyyy}.pdf");
        string templatePath = TemplatePathResolver.ResolveTemplatePath(templateFileName, nameof(DocumentGenerationService));

        AppLogger.LogInfo($"[DocumentGenerationService] Copying authorization from '{templatePath}' to '{outputPath}' for AMEF {amef.Series}");

        EnsureDirectoryExists(outputPath);
        File.Copy(templatePath, outputPath, overwrite: true);

        AppLogger.LogInfo($"[DocumentGenerationService] Authorization copied successfully to '{outputPath}'");
        return Task.FromResult(outputPath);
    }

    public virtual Task<string> GenerateWarrantyAsync(Amef amef, string outputPath)
    {
        if (amef == null) throw new ArgumentNullException(nameof(amef));
        if (amef.Bill == null) throw new ArgumentException("AMEF has no associated Bill.", nameof(amef));
        if (string.IsNullOrWhiteSpace(amef.GetBrand())) throw new ArgumentException("AMEF has no Brand specified in its Authorization.", nameof(amef));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));

        string templateFileName = Path.Combine("Warranties", $"Garantie{amef.GetBrand()}.pdf");
        string templatePath = TemplatePathResolver.ResolveTemplatePath(templateFileName, nameof(DocumentGenerationService));

        AppLogger.LogInfo($"[DocumentGenerationService] Generating warranty from template '{templatePath}' to '{outputPath}' for AMEF {amef.Series}");

        return Task.Run(() =>
        {
            EnsureDirectoryExists(outputPath);

            try
            {
                using (var reader = new PdfReader(templatePath))
                using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    PdfStamper? stamper = null;
                    try
                    {
                        stamper = new PdfStamper(reader, outputStream);
                        var form = stamper.AcroFields;

                        string billDateStr = amef.Bill.BillDate.ToString("dd.MM.yyyy");
                        string deviceTypeStr = amef.GetDeviceType();
                        string modelStr = amef.Model ?? string.Empty;
                        string seriesStr = amef.Series ?? string.Empty;
                        string invoiceStr = $"{amef.Bill.BillSeries} {amef.Bill.BillNumber} / {billDateStr}";

                        form.SetField("data", billDateStr);
                        form.SetField("tip", deviceTypeStr);
                        form.SetField("model", modelStr);
                        form.SetField("serie", seriesStr);
                        form.SetField("factura", invoiceStr);

                        stamper.FormFlattening = true;
                        stamper.Close();
                        stamper = null;
                    }
                    finally
                    {
                        try { stamper?.Close(); } catch { }
                        try { reader.Close(); } catch { }
                    }
                }

                AppLogger.LogInfo($"[DocumentGenerationService] Warranty PDF successfully generated at '{outputPath}'");
                return outputPath;
            }
            catch (Exception ex)
            {
                AppLogger.LogException(ex, "DocumentGenerationService.GenerateWarrantyAsync");
                try
                {
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                }
                catch { }
                throw;
            }
        });
    }

    public virtual Task<string> GenerateTrainingSheetAsync(Amef amef, string outputPath)
    {
        if (amef == null) throw new ArgumentNullException(nameof(amef));
        var client = amef.Contract?.Client;
        if (client == null) throw new ArgumentException("AMEF has no associated Client through Contract.", nameof(amef));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));

        string templateFileName = "Fisa instruire.pdf";
        string templatePath = TemplatePathResolver.ResolveTemplatePath(templateFileName, nameof(DocumentGenerationService));

        AppLogger.LogInfo($"[DocumentGenerationService] Generating training sheet from template '{templatePath}' to '{outputPath}' for AMEF {amef.Series}");

        return Task.Run(() =>
        {
            EnsureDirectoryExists(outputPath);

            try
            {
                using (var reader = new PdfReader(templatePath))
                using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    PdfStamper? stamper = null;
                    try
                    {
                        stamper = new PdfStamper(reader, outputStream);
                        var form = stamper.AcroFields;

                        var person = client.Person;
                        string repName = person != null ? $"{person.LastName} {person.FirstName}".Trim() : string.Empty;
                        string clientName = client.Name ?? string.Empty;
                        string regNumber = client.RegistrationNumber ?? string.Empty;
                        string cui = client.GetFormattedCui();
                        string modelStr = amef.Model ?? string.Empty;
                        string seriesStr = amef.Series ?? string.Empty;

                        form.SetField("Societate1", clientName);
                        form.SetField("Societate2", clientName);
                        form.SetField("ORC", regNumber);
                        form.SetField("CUI", cui);
                        form.SetField("AMEF", modelStr);
                        form.SetField("Serie", seriesStr);
                        form.SetField("Nume", repName);

                        stamper.FormFlattening = true;
                        stamper.Close();
                        stamper = null;
                    }
                    finally
                    {
                        try { stamper?.Close(); } catch { }
                        try { reader.Close(); } catch { }
                    }
                }

                AppLogger.LogInfo($"[DocumentGenerationService] Training sheet PDF successfully generated at '{outputPath}'");
                return outputPath;
            }
            catch (Exception ex)
            {
                AppLogger.LogException(ex, "DocumentGenerationService.GenerateTrainingSheetAsync");
                try
                {
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                }
                catch { }
                throw;
            }
        });
    }
}
