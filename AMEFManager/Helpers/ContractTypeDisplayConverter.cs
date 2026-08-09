using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class ContractTypeDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = values.Count > 0 ? values[0] as string : null;
        var valueParam = values.Count > 1 ? values[1] : null;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
            parts.Add(name);

        if (valueParam is int val && val > 0)
            parts.Add(val.ToString());

        return string.Join(" - ", parts);
    }
}
