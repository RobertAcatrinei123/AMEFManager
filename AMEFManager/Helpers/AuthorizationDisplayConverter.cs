using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class AuthorizationDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var number = values.Count > 0 ? values[0] : null;
        var date = values.Count > 1 ? values[1] : null;
        var model = values.Count > 2 ? values[2] as string : null;

        var parts = new List<string>();

        if (number is int num && num > 0)
            parts.Add($"Nr. {num}");

        if (date is DateTimeOffset dto)
            parts.Add(dto.ToString("dd.MM.yyyy", culture));
        else if (date is DateOnly dateOnly)
            parts.Add(dateOnly.ToString("dd.MM.yyyy", culture));

        if (!string.IsNullOrWhiteSpace(model))
            parts.Add(model);

        return string.Join(" / ", parts);
    }
}
