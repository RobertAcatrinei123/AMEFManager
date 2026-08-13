using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AMEFManager.Models;

namespace AMEFManager.Helpers;

public class SealingDocumentDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SealingDocument s)
        {
            var amefSeries = s.Amef != null ? s.Amef.Series : string.Empty;
            return $"Nr. {s.Number} / {s.Date:dd.MM.yyyy} - {amefSeries}";
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
