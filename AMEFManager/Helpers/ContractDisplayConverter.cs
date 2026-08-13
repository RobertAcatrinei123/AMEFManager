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
        if (values == null || values.Count == 0)
            return string.Empty;

        var number = values.Count > 0 ? values[0] : null;
        var date = values.Count > 1 ? values[1] : null;
        var typeName = values.Count > 2 ? values[2] as string : null;
        var value = values.Count > 3 ? values[3] : null;
        var clientName = values.Count > 4 ? values[4] as string : null;

        var parts = new List<string>();

        if (number is int num && num > 0)
            parts.Add($"Nr. {num}");

        if (date is DateTimeOffset dto)
            parts.Add(dto.ToString("dd.MM.yyyy", culture));
        else if (date is DateOnly dateOnly)
            parts.Add(dateOnly.ToString("dd.MM.yyyy", culture));
        else if (date is DateTime dt)
            parts.Add(dt.ToString("dd.MM.yyyy", culture));

        var contractPart = string.Join(" / ", parts);

        if (!string.IsNullOrWhiteSpace(clientName))
            contractPart = string.IsNullOrWhiteSpace(contractPart) ? clientName : $"{contractPart} - {clientName}";

        if (!string.IsNullOrWhiteSpace(typeName))
            contractPart = string.IsNullOrWhiteSpace(contractPart) ? typeName : $"{contractPart} - {typeName}";

        string? valStr = null;
        if (value is int intVal && intVal > 0)
            valStr = intVal.ToString();
        else if (value is decimal decVal && decVal > 0)
            valStr = decVal.ToString(culture);
        else if (value is double dVal && dVal > 0)
            valStr = dVal.ToString(culture);
        else if (value is string sVal && !string.IsNullOrWhiteSpace(sVal))
            valStr = sVal;
        else if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
            valStr = value.ToString();

        if (!string.IsNullOrWhiteSpace(valStr))
            contractPart = string.IsNullOrWhiteSpace(contractPart) ? valStr : $"{contractPart} - {valStr}";

        return contractPart;
    }
}
