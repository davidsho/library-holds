namespace LibraryHolds.Api.Time;

/// <summary>The library keeps UK time, so collect-by times are worked out and shown in it.</summary>
public static class UkTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static DateTimeOffset From(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);
}
