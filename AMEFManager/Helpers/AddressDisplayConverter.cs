using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class AddressDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // values: City, Street, StreetNumber, Block, Floor, Apartment, County, Other
        var city = values.Count > 0 ? values[0] as string : null;
        var street = values.Count > 1 ? values[1] as string : null;
        var streetNumber = values.Count > 2 ? values[2] as string : null;
        var block = values.Count > 3 ? values[3] as string : null;
        var floor = values.Count > 4 ? values[4] as string : null;
        var apartment = values.Count > 5 ? values[5] as string : null;
        var county = values.Count > 6 ? values[6] as string : null;
        var other = values.Count > 7 ? values[7] as string : null;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(city))
            parts.Add(city);

        if (!string.IsNullOrWhiteSpace(street))
            parts.Add(street);

        if (!string.IsNullOrWhiteSpace(streetNumber))
            parts.Add($"NR. {streetNumber}");

        if (!string.IsNullOrWhiteSpace(block))
            parts.Add($"BLOC {block}");

        if (!string.IsNullOrWhiteSpace(floor))
            parts.Add($"ET. {floor}");

        if (!string.IsNullOrWhiteSpace(apartment))
            parts.Add($"AP. {apartment}");

        if (!string.IsNullOrWhiteSpace(county))
            parts.Add($"JUD. {county}");

        if (!string.IsNullOrWhiteSpace(other))
            parts.Add(other);

        return string.Join(", ", parts);
    }
}
