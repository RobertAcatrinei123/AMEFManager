using System;
using Microsoft.VisualBasic;

namespace AMEFManager.Models;

public class Person
{
    public int Id { get; set; }
    public required string LastName { get; set; }
    public required string FirstName { get; set; }
    public required string Cnp { get; set; }
    
    public string? Email { get; set; }
    public string? Phone { get; set; }
    
    public required string Series { get; set; }
    public required string Number { get; set; }
    public required string Issuer { get; set; }
    public required DateOnly IssuingDate { get; set; }
    
    public required string Role { get; set; }
    
    public int AddressId { get; set; }
    public Address Address { get; set; } = null!;
}