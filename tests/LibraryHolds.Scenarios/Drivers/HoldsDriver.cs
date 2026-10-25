using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryHolds.Scenarios.Model;
using LibraryHolds.Scenarios.Support;
using LibraryHolds.Scenarios.Support.App;

namespace LibraryHolds.Scenarios.Drivers;

/// <summary>Placing, cancelling and checking holds, through the holds API.</summary>
public sealed class HoldsDriver(LibraryHoldsApp app)
{
    public async Task<PlacedHold> PlaceAsync(string member, string title, string format, DateTimeOffset at)
    {
        app.Clock.UtcNow = at;
        using var response = await app.Api.PostAsJsonAsync("/holds", new { memberId = member, title, format });
        response.StatusCode.Should().Be(HttpStatusCode.Created, "placing {0}'s hold should succeed", member);

        var hold = await response.ReadStrictJsonAsync<HoldJson>();
        return new PlacedHold(hold.Id, member, title, format, at);
    }

    public async Task CancelAsync(PlacedHold hold)
    {
        using var response = await app.Api.DeleteAsync($"/holds/{hold.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "cancelling {0}'s hold should succeed", hold.Member);
    }

    /// <summary>For a Given that must have taken effect before the scenario can go on.</summary>
    public async Task RequireReadyForCollectionAsync(PlacedHold hold, ReturnedCopy copy)
    {
        try
        {
            await AssertReadyForCollectionAsync(hold, copy, Wire.Instant(ExpectedCollectBy.For(copy.ReturnedAt)));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Precondition failed: {e.Message}", e);
        }
    }

    public Task AssertWaitingAsync(PlacedHold hold) =>
        AssertIsAsync(hold, Expected(hold, "Waiting"), "{0}'s hold should still be waiting");

    public Task AssertCancelledAsync(PlacedHold hold) =>
        AssertIsAsync(hold, Expected(hold, "Cancelled"), "{0}'s hold should still be cancelled");

    public Task AssertReadyForCollectionAsync(PlacedHold hold, ReturnedCopy copy, string collectBy) =>
        AssertIsAsync(
            hold,
            Expected(hold, "ReadyForCollection") with { CopyId = copy.Id, CollectBy = collectBy },
            "{0}'s hold should be ready for collection with the copy returned at {1}, until {2}",
            Wire.Instant(copy.ReturnedAt),
            collectBy);

    /// <summary>
    /// Compares the whole hold, so a field the API gets wrong fails here even when no step mentions it.
    /// The id is the one the API gave when the hold was placed, which also checks this is the same hold.
    /// </summary>
    private async Task AssertIsAsync(PlacedHold hold, HoldJson expected, string because, params object[] becauseArgs)
    {
        await app.WaitUntilSettledAsync();
        var actual = await app.Api.GetFromStrictJsonAsync<HoldJson>($"/holds/{hold.Id}");
        actual.Should().BeEquivalentTo(expected, o => o.ComparingRecordsByMembers(), because, [hold.Member, .. becauseArgs]);
    }

    private static HoldJson Expected(PlacedHold hold, string status) => new()
    {
        Id = hold.Id,
        MemberId = hold.Member,
        Title = hold.Title,
        Format = hold.Format,
        Status = status,
        PlacedAt = Wire.Instant(hold.PlacedAt),
        CopyId = null,
        CollectBy = null
    };
}
