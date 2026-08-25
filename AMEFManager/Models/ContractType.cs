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

    public string GetBillingRules()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return string.Empty;

        var trimmed = Name.Trim();
        if (string.Equals(trimmed, "sezonier", StringComparison.OrdinalIgnoreCase))
            return "Facturile vor fi emise de Executant, in decursul sezonului estival";
        if (string.Equals(trimmed, "anual", StringComparison.OrdinalIgnoreCase))
            return "Facturile vor fi emise de Executant, anual, in prima luna a fiecarui an de contract.";
        if (string.Equals(trimmed, "lunar", StringComparison.OrdinalIgnoreCase))
            return "Facturile vor fi emise de Executant, lunar, cel mai tarziu in ultima zi lucratoare a fiecarei luni pentru serviciile prestate in luna curenta.";

        return string.Empty;
    }
}