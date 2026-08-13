using System.Threading.Tasks;
using AMEFManager.Models;

namespace AMEFManager.Services;

public interface IDocumentGenerationService
{
    Task<string> GeneratePdfAsync(string xmlPath, string templatePdfPath, string outputPdfPath);
    Task<string> GenerateServiceContractAsync(Contract contract, string outputPath);
    Task<string> GeneratePaperContractAsync(Contract contract, string outputPath);
    Task<string> GenerateContractAnnexAsync(AdditionalDocument additionalDocument, string outputPath);
    Task<string> GenerateInstallationDeclarationAsync(Amef amef, string outputPath, bool isRefiscalizare = false, string? reason = null);
    Task<string> CopyAuthorizationAsync(Amef amef, string outputPath);
    Task<string> GenerateWarrantyAsync(Amef amef, string outputPath);
    Task<string> GenerateTrainingSheetAsync(Amef amef, string outputPath);
    Task<string> GenerateSealingDocumentAsync(Amef amef, string outputPath, int number, System.DateOnly? date = null);
    Task<string> GenerateDeliveryDocumentAsync(Amef amef, string outputPath, int number, string? reason = null, System.DateOnly? date = null);
}
