using System;

namespace AMEFManager.Models;

public class AdditionalDocument
{
    public int Id { get; set; }
    public int Number { get; set; }
    public required DateOnly Date { get; set; }
    
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
}