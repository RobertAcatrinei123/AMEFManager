using System;

namespace AMEFManager.Helpers;

public static class DateHelper
{
    public static DateTimeOffset? ToDateTimeOffset(DateOnly? date)
    {
        if (!date.HasValue || date.Value == default || date.Value == DateOnly.MinValue)
            return null;

        return new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    public static DateTimeOffset? ToDateTimeOffset(DateOnly date)
    {
        if (date == default || date == DateOnly.MinValue)
            return null;

        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    public static DateOnly? ToDateOnly(DateTimeOffset? dto)
    {
        if (!dto.HasValue)
            return null;

        return DateOnly.FromDateTime(dto.Value.Date);
    }
}
