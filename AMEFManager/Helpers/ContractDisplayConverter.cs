using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class ContractDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var number = values.Count > 0 ? values[0] : null;
        var date = values.Count > 1 ? values[1] : null;
        var typeName = values.Count > 2 ? values[2] as string : null;

        var parts = new List<string>();

        if (number is int num && num > 0)
            parts.Add($"Nr. {num}");

        if (date is DateTimeOffset dto)
            parts.Add(dto.ToString("dd.MM.yyyy", culture));
        else if (date is DateOnly dateOnly)
            parts.Add(dateOnly.ToString("dd.MM.yyyy", culture));

        var contractPart = string.Join(" / ", parts);
        if (!string.IsNullOrWhiteSpace(typeName))
            contractPart = string.IsNullOrWhiteSpace(contractPart) ? typeName : $"{contractPart} - {typeName}";

        return contractPart;
    }
}
