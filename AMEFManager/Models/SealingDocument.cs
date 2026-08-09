using System;

namespace AMEFManager.Models;

public class SealingDocument
{
    public int Id { get; set; }
    public required int Number { get; set; }
    public required DateOnly Date { get; set; }
    
    public int AmefId { get; set; }
    public Amef Amef { get; set; } = null!;
}