using System;

namespace AMEFManager.Models;

public class ContractType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; } 

    public string GetFrequency()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "COMPLETEAZA MANUAL";

        var trimmed = Name.Trim();
        if (string.Equals(trimmed, "lunar", StringComparison.OrdinalIgnoreCase))
            return "luna";
        if (string.Equals(trimmed, "anual", StringComparison.OrdinalIgnoreCase))
            return "an";
        if (string.Equals(trimmed, "sezonier", StringComparison.OrdinalIgnoreCase))
            return "sezon";

        return "COMPLETEAZA MANUAL";
    }
}