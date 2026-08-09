using System;

namespace AMEFManager.Models;

public class Bill
{
    public int Id { get; set; }
    
    public required DateOnly BillDate { get; set; }
    public required string BillSeries { get; set; }
    public int BillNumber { get; set; }
}