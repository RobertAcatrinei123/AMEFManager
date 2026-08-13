using System;
using System.Collections.Generic;

namespace AMEFManager.Models;

public class Contract
{
    public int Id { get; set; }
    public int Number { get; set; }
    public required DateOnly Date { get; set; }
    
    public int ContractTypeId { get; set; }
    public ContractType Type { get; set; } = null!;
    
    public bool IsActive { get; set; }
    public required DateTime ValidUntil { get; set; }
    
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    
    public List<Amef> Amefs { get; set; } = new();
    public List<AdditionalDocument> AdditionalDocuments { get; set; } = new();
    
    public string DisplayName => $"Contract {Number} - {Client?.Name ?? "Fara client"}";
}