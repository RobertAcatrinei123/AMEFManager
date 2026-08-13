using System;

namespace AMEFManager.Models;

public class AdditionalDocument
{
    public int Id { get; set; }
    public int Number { get; set; }
    public required DateOnly Date { get; set; }
    
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
}