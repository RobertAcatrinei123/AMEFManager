namespace AMEFManager.Models;

public class Client
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NationalIdentifier { get; set; }
    public required string RegistrationNumber { get; set; }
    
    public int AddressId { get; set; }
    
    public Address Address { get; set; } = null!;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
}