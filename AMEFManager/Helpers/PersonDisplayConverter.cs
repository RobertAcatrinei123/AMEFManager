using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class PersonDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var lastName = values.Count > 0 ? values[0] as string : null;
        var firstName = values.Count > 1 ? values[1] as string : null;
        
        var parts = new List<string>();

        var name = string.Join(" ", new[] { lastName, firstName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(name))
            parts.Add(name);

        return string.Join(", ", parts);
    }
}
