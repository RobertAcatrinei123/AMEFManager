using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class BillDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var billSeries = values.Count > 0 ? values[0] as string : null;
        var billNumber = values.Count > 1 ? values[1] : null;
        var billDate = values.Count > 2 ? values[2] : null;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(billSeries))
            parts.Add(billSeries);

        if (billNumber is int num && num != 0)
            parts.Add(num.ToString());

        if (billDate is DateTimeOffset dto)
            parts.Add(dto.ToString("dd.MM.yyyy"));

        return string.Join(" / ", parts);
    }
}
