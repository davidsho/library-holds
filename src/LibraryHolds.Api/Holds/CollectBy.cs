using LibraryHolds.Api.Time;

namespace LibraryHolds.Api.Holds;

/// <summary>
/// The member can collect until closing time, 20:00 UK time, on the 7th day after the copy reaches
/// the hold shelf.
/// </summary>
public static class CollectBy
{
    public static readonly TimeOnly ClosingTime = new(20, 0);
    private const int DaysOnTheHoldShelf = 7;

    public static DateTimeOffset For(DateTimeOffset reachedTheHoldShelf)
    {
        var shelvedOn = DateOnly.FromDateTime(UkTime.From(reachedTheHoldShelf).DateTime);
        var closing = shelvedOn.AddDays(DaysOnTheHoldShelf).ToDateTime(ClosingTime);

        // 20:00 is never skipped or repeated by a clock change, so its offset is never in doubt.
        return new DateTimeOffset(closing, UkTime.Zone.GetUtcOffset(closing));
    }
}
