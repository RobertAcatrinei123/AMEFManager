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

    public string GetFullAddress()
    {
        var parts = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);
        if (!string.IsNullOrWhiteSpace(Street)) parts.Add(Street);
        if (!string.IsNullOrWhiteSpace(StreetNumber)) parts.Add($"NR. {StreetNumber}");
        if (!string.IsNullOrWhiteSpace(Block)) parts.Add($"BLOC {Block}");
        if (!string.IsNullOrWhiteSpace(Floor)) parts.Add($"ET. {Floor}");
        if (!string.IsNullOrWhiteSpace(Apartment)) parts.Add($"AP. {Apartment}");
        if (!string.IsNullOrWhiteSpace(County)) parts.Add($"JUD. {County}");
        if (!string.IsNullOrWhiteSpace(Other)) parts.Add(Other);

        return string.Join(", ", parts);
    }

    public string GetRestAddress()
    {
        var parts = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrWhiteSpace(StreetNumber)) parts.Add(StreetNumber);
        if (!string.IsNullOrWhiteSpace(Block)) parts.Add(Block);
        if (!string.IsNullOrWhiteSpace(Floor)) parts.Add(Floor);
        if (!string.IsNullOrWhiteSpace(Apartment)) parts.Add(Apartment);
        if (!string.IsNullOrWhiteSpace(Other)) parts.Add(Other);

        return string.Join(", ", parts);
    }
}