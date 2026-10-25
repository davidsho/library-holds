using FluentAssertions;
using LibraryHolds.Scenarios.Model;
using LibraryHolds.Scenarios.Support;
using LibraryHolds.Scenarios.Support.App;

namespace LibraryHolds.Scenarios.Drivers;

/// <summary>What the notification provider was asked to tell members.</summary>
public sealed class NotificationsDriver(LibraryHoldsApp app)
{
    public async Task AssertToldTheCopyIsReadyAsync(PlacedHold hold, ReturnedCopy copy)
    {
        await app.WaitUntilSettledAsync();
        var toMember = Messages().Where(m => m.MemberId == hold.Member).ToList();

        // The outbox delivers at least once, so a repeat of the same message is allowed; the first is checked.
        toMember.Should().NotBeEmpty("{0} should have been told the copy is ready", hold.Member);
        toMember[0].Should().BeEquivalentTo(
            new HoldReadyMessageJson
            {
                MemberId = hold.Member,
                HoldId = hold.Id,
                Title = hold.Title,
                Format = hold.Format,
                CollectBy = Wire.Instant(ExpectedCollectBy.For(copy.ReturnedAt))
            },
            o => o.ComparingRecordsByMembers(),
            "{0} should be told the copy returned at {1} is ready",
            hold.Member,
            Wire.Instant(copy.ReturnedAt));
    }

    /// <summary>Only meaningful after the app has settled, which is why this waits first.</summary>
    public async Task AssertNobodyToldAnythingAsync()
    {
        await app.WaitUntilSettledAsync();
        Messages().Should().BeEmpty("nobody should have been told anything");
    }

    private IEnumerable<HoldReadyMessageJson> Messages() =>
        app.NotificationProvider.HoldReadyMessages().Select(StrictJson.Parse<HoldReadyMessageJson>);
}
