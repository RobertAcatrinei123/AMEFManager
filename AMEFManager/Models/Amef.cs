using System;
using System.Collections.Generic;

namespace AMEFManager.Models;

public class Amef
{
    public int Id  { get; set; }
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
    
    public string? ServicePassword { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    
    public ICollection<C802Document> C802Documents { get; set; } = new List<C802Document>();

    public string? Model => Authorization?.Model;

    public string GetBrand() => Authorization?.Brand ?? string.Empty;

    public string GetDeviceType() => Authorization?.DeviceType ?? string.Empty;

    public string GetConfig() => Authorization?.Configuration ?? string.Empty;
}
