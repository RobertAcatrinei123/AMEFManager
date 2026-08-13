using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class AuthorizationDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0)
            return string.Empty;

        string? brand = null;
        string? model = null;
        int? number = null;
        string? dateStr = null;

        // If standard 4-param format: (Brand, Model, Number, Date)
        if (values.Count >= 4 && values[0] is string b && values[1] is string m)
        {
            brand = b;
            model = m;
            if (values[2] is int num) number = num;
            if (values[3] is DateTimeOffset dto) dateStr = dto.ToString("dd.MM.yyyy", culture);
            else if (values[3] is DateOnly dOnly) dateStr = dOnly.ToString("dd.MM.yyyy", culture);
            else if (values[3] is DateTime dt) dateStr = dt.ToString("dd.MM.yyyy", culture);
        }
        else
        {
            // Flexible extraction for legacy or varied argument sets
            foreach (var val in values)
            {
                if (val == null) continue;
                if (val is int num && num > 0) number = num;
                else if (val is DateTimeOffset dto) dateStr = dto.ToString("dd.MM.yyyy", culture);
                else if (val is DateOnly dOnly) dateStr = dOnly.ToString("dd.MM.yyyy", culture);
                else if (val is DateTime dt) dateStr = dt.ToString("dd.MM.yyyy", culture);
                else if (val is string str && !string.IsNullOrWhiteSpace(str))
                {
                    if (model == null) model = str;
                    else if (brand == null) brand = str;
                }
            }
        }

        var parts = new List<string>();

        var devParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(brand)) devParts.Add(brand);
        if (!string.IsNullOrWhiteSpace(model)) devParts.Add(model);

        if (devParts.Count > 0)
            parts.Add(string.Join(" ", devParts));

        if (number.HasValue && number.Value > 0)
            parts.Add($"Nr. {number.Value}");

        if (!string.IsNullOrWhiteSpace(dateStr))
            parts.Add(dateStr);

        return string.Join(" / ", parts);
    }
}
