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
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
            parts.Add(name);

        return string.Join(", ", parts);
    }
}
