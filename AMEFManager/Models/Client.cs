namespace AMEFManager.Models;

public class Client
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NationalIdentifier { get; set; }
    public required string RegistrationNumber { get; set; }
    public bool PaysTVA { get; set; }
    
    public int AddressId { get; set; }
    
    public Address Address { get; set; } = null!;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string FormattedCui => GetFormattedCui();

    public string GetFormattedCui()
    {
        if (string.IsNullOrWhiteSpace(NationalIdentifier))
            return string.Empty;

        var raw = NationalIdentifier.Trim();
        if (raw.StartsWith("RO", System.StringComparison.OrdinalIgnoreCase))
            raw = raw[2..].Trim();

        return PaysTVA ? $"RO{raw}" : raw;
    }
}