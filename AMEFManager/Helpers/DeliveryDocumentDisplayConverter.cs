using System;
using System.Globalization;
using AMEFManager.Models;
using Avalonia.Data.Converters;

namespace AMEFManager.Helpers;

public class DeliveryDocumentDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DeliveryDocument doc)
        {
            var dateStr = doc.Date.ToString("dd.MM.yyyy");
            var modelStr = doc.Amef?.Model ?? string.Empty;
            return $"Nr. {doc.Number} / {dateStr} - {modelStr}";
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
