using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class AdditionalDocumentDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var nr = values.Count > 0 ? values[0] : null;
        var date = values.Count > 1 ? values[1] : null;
        var contractName = values.Count > 2 ? values[2] as string : null;

        var parts = new List<string>();

        if (nr is int nrInt && nrInt > 0)
            parts.Add($"Nr. {nrInt}");
        if (date is DateTimeOffset dto)
            parts.Add(dto.ToString("dd.MM.yyyy", culture));
        else if (date is DateOnly dateOnly)
            parts.Add(dateOnly.ToString("dd.MM.yyyy", culture));

        var docPart = string.Join(" / ", parts);
        if (!string.IsNullOrWhiteSpace(contractName))
            docPart = string.IsNullOrWhiteSpace(docPart) ? contractName : $"{docPart} - {contractName}";

        return docPart;
    }
}
