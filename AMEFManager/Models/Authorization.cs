using System;

namespace AMEFManager.Models;

public class Authorization
{
    public int Id { get; set; }
    
    public int Number { get; set; }
    public required DateOnly Date { get; set; }
    public required string Model { get; set; }
    public required string Brand { get; set; }
    public required string DeviceType { get; set; }
    public required string Configuration { get; set; }
}