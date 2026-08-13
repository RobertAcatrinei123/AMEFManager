using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class AmefDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var model = values.Count > 0 ? values[0] as string : null;
        var series = values.Count > 1 ? values[1] as string : null;
        var nui = values.Count > 2 ? values[2] as string : null;
        var clientName = values.Count > 3 ? values[3] as string : null;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(model)) parts.Add(model);
        if (!string.IsNullOrWhiteSpace(clientName)) parts.Add(clientName);
        if (!string.IsNullOrWhiteSpace(series)) parts.Add(series);
        
        var main = string.Join(" - ", parts);
        if (!string.IsNullOrWhiteSpace(nui))
        {
            main = string.IsNullOrWhiteSpace(main) ? $"({nui})" : $"{main} ({nui})";
        }
        
        return main;
    }
}
