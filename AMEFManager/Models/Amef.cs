using System;

namespace AMEFManager.Models;

public class Amef
{
    public int Id  { get; set; }
    
    public required string Model { get; set; }
    public required string Series { get; set; }
    public required string NUI { get; set; }
    
    public required string FiscalCity { get; set; } = string.Empty;
    public DateOnly FiscalizationDate { get; set; }
    
    public string? ConnectionMethod { get; set; }
    public DateOnly? ConnectionExpirationDate { get; set; }
    
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    
    public int AddressId { get; set; }
    public Address Address { get; set; } = null!;
    
    public int AuthorizationId { get; set; }
    public Authorization Authorization { get; set; } = null!;
    
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
}
