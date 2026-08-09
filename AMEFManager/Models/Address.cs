namespace AMEFManager.Models;

public class Address
{
    public int Id { get; set; }
    public string? County { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? StreetNumber { get; set; }
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Other { get; set; }
}