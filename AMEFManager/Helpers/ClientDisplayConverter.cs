using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class ClientDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = values.Count > 0 ? values[0] as string : null;
        var nationalIdentifier = values.Count > 1 ? values[1] as string : null;
        var registrationNumber = values.Count > 2 ? values[2] as string : null;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
            parts.Add(name);

        if (!string.IsNullOrWhiteSpace(nationalIdentifier))
            parts.Add($"CUI: {nationalIdentifier}");

        if (!string.IsNullOrWhiteSpace(registrationNumber))
            parts.Add($"Reg: {registrationNumber}");

        return string.Join(", ", parts);
    }
}
