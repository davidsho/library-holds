using NodaTime;
using NodaTime.Extensions;

namespace LibraryHolds.Scenarios.Support;

/// <summary>
/// The collect-by rule worked out independently of the API's own code, with NodaTime and its bundled
/// time zone database, so a mistake in the API's version can't also be in the expectation.
/// </summary>
public static class ExpectedCollectBy
{
    private static readonly DateTimeZone London = DateTimeZoneProviders.Tzdb["Europe/London"];
    private static readonly LocalTime ClosingTime = new(20, 0);

    public static DateTimeOffset For(DateTimeOffset reachedTheHoldShelf)
    {
        var shelvedOn = reachedTheHoldShelf.ToInstant().InZone(London).Date;
        return (shelvedOn.PlusDays(7) + ClosingTime).InZoneStrictly(London).ToDateTimeOffset();
    }
}
