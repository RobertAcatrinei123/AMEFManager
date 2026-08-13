using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AMEFManager.Data;

public static class DateParsingHelper
{
    private static readonly string[] DateOnlyFormats =
    {
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "dd/MM/yyyy",
        "d.M.yyyy",
        "d/M/yyyy",
        "yyyy/MM/dd",
        "yyyy.MM.dd",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fffffff",
        "dd.MM.yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss"
    };

    private static readonly string[] DateTimeFormats =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fffffff",
        "yyyy-MM-dd",
        "dd.MM.yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "dd.MM.yyyy",
        "dd/MM/yyyy",
        "d.M.yyyy",
        "d/M/yyyy",
        "yyyy/MM/dd",
        "yyyy.MM.dd"
    };

    public static DateOnly ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        var trimmed = value.Trim();

        if (DateOnly.TryParseExact(trimmed, DateOnlyFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dExact))
            return dExact;

        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dInv))
            return dInv;

        if (DateTime.TryParseExact(trimmed, DateOnlyFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
            return DateOnly.FromDateTime(dtExact);

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtInv))
            return DateOnly.FromDateTime(dtInv);

        if (DateOnly.TryParse(trimmed, out var dFallback))
            return dFallback;

        if (DateTime.TryParse(trimmed, out var dtFallback))
            return DateOnly.FromDateTime(dtFallback);

        return default;
    }

    public static DateOnly? ParseNullableDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return ParseDateOnly(value);
    }

    public static DateTime ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        var trimmed = value.Trim();

        if (DateTime.TryParseExact(trimmed, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
            return dtExact;

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtInv))
            return dtInv;

        if (DateTime.TryParse(trimmed, out var dtFallback))
            return dtFallback;

        return default;
    }

    public static DateTime? ParseNullableDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return ParseDateTime(value);
    }
}

public class DateOnlyConverter : ValueConverter<DateOnly, string>
{
    public DateOnlyConverter() : base(
        d => d.ToString("yyyy-MM-dd"),
        s => DateParsingHelper.ParseDateOnly(s))
    {
    }
}

public class NullableDateOnlyConverter : ValueConverter<DateOnly?, string?>
{
    public NullableDateOnlyConverter() : base(
        d => d.HasValue ? d.Value.ToString("yyyy-MM-dd") : null,
        s => DateParsingHelper.ParseNullableDateOnly(s))
    {
    }
}

public class DateTimeConverter : ValueConverter<DateTime, string>
{
    public DateTimeConverter() : base(
        d => d.ToString("yyyy-MM-dd HH:mm:ss"),
        s => DateParsingHelper.ParseDateTime(s))
    {
    }
}

public class NullableDateTimeConverter : ValueConverter<DateTime?, string?>
{
    public NullableDateTimeConverter() : base(
        d => d.HasValue ? d.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
        s => DateParsingHelper.ParseNullableDateTime(s))
    {
    }
}
