using NodaTime;
using NodaTime.Extensions;

namespace LibraryHolds.Scenarios.Support.Randomised;

/// <summary>
/// Random instants, in whole seconds because that's what the Gherkin and the API's JSON use. Holds are
/// placed in 2026 and copies returned from 2027, so a return always comes after the holds it could fill.
/// </summary>
public sealed class RandomInstants(ScenarioRandom random)
{
    private static readonly DateTimeZone London = DateTimeZoneProviders.Tzdb["Europe/London"];
    private static readonly DateTimeOffset HoldsFrom = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReturnsFrom = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReturnsUntil = new(2036, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>One instant per hold, in the order the holds are placed, at least a second apart.</summary>
    public IReadOnlyList<DateTimeOffset> HoldsPlacedOneAfterAnother(int count)
    {
        var placed = new List<DateTimeOffset>();
        var next = Between(HoldsFrom, HoldsFrom.AddDays(180));
        for (var i = 0; i < count; i++)
        {
            placed.Add(next);
            next = next.AddSeconds(random.Randomizer.Int(1, 7 * 24 * 60 * 60));
        }

        return placed;
    }

    /// <summary>When a copy is returned. The mode aims the randomness at the ranges the collect-by rule finds hardest.</summary>
    public DateTimeOffset Return(string mode) => mode switch
    {
        "at a random time" => Between(ReturnsFrom, ReturnsUntil),
        "in the week before a clock change" => InTheWeekBeforeAClockChange(),
        "around midnight UK time" => AroundMidnightUkTime(),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Not a kind of random return time the scenarios know.")
    };

    // UK clocks change at 01:00 UTC on the last Sunday of March and of October.
    private DateTimeOffset InTheWeekBeforeAClockChange()
    {
        var year = random.Randomizer.Int(ReturnsFrom.Year, ReturnsUntil.Year - 1);
        var month = random.Randomizer.Bool() ? 3 : 10;
        var lastSunday = new DateTime(year, month, DateTime.DaysInMonth(year, month), 1, 0, 0, DateTimeKind.Utc);
        while (lastSunday.DayOfWeek != DayOfWeek.Sunday)
        {
            lastSunday = lastSunday.AddDays(-1);
        }

        var change = new DateTimeOffset(lastSunday);
        return Between(change.AddDays(-7), change);
    }

    private DateTimeOffset AroundMidnightUkTime()
    {
        var day = Between(ReturnsFrom, ReturnsUntil).ToInstant().InZone(London).Date;
        var midnight = day.AtStartOfDayInZone(London).ToDateTimeOffset().ToUniversalTime();
        return Between(midnight.AddMinutes(-90), midnight.AddMinutes(90));
    }

    private DateTimeOffset Between(DateTimeOffset from, DateTimeOffset until) =>
        from.AddSeconds(random.Randomizer.Long(0, (long)(until - from).TotalSeconds - 1));
}
