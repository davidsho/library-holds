using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryHolds.Scenarios.Model;
using LibraryHolds.Scenarios.Support;
using LibraryHolds.Scenarios.Support.App;

namespace LibraryHolds.Scenarios.Drivers;

/// <summary>Returning copies and checking where they went, through the returns and copies APIs.</summary>
public sealed class ReturnsDriver(LibraryHoldsApp app)
{
    /// <summary>A new copy, with a barcode no earlier step has used.</summary>
    public static ReturnedCopy NewCopy(string title, string format, DateTimeOffset at) => new(Guid.NewGuid(), title, format, at);

    /// <summary>Only acts. Whether the copy fills a hold is for the Then steps to check.</summary>
    public async Task ReturnAsync(ReturnedCopy copy)
    {
        app.Clock.UtcNow = copy.ReturnedAt;
        using var response = await app.Api.PostAsJsonAsync("/returns", new { copyId = copy.Id, title = copy.Title, format = copy.Format });
        response.StatusCode.Should().Be(HttpStatusCode.OK, "returning the copy of {0} should succeed", copy.Title);
    }

    public Task AssertOnTheHoldShelfForAsync(ReturnedCopy copy, PlacedHold hold) =>
        AssertIsAsync(copy, Expected(copy, "HoldShelf") with { HoldId = hold.Id }, "the copy should be on the hold shelf for {0}", hold.Member);

    public Task AssertBackOnTheShelfAsync(ReturnedCopy copy) =>
        AssertIsAsync(copy, Expected(copy, "Shelf"), "the copy of {0} should be back on the shelf", copy.Title);

    private async Task AssertIsAsync(ReturnedCopy copy, CopyJson expected, string because, params object[] becauseArgs)
    {
        await app.WaitUntilSettledAsync();
        var actual = await app.Api.GetFromStrictJsonAsync<CopyJson>($"/copies/{copy.Id}");
        actual.Should().BeEquivalentTo(expected, o => o.ComparingRecordsByMembers(), because, becauseArgs);
    }

    private static CopyJson Expected(ReturnedCopy copy, string location) => new()
    {
        Id = copy.Id,
        Title = copy.Title,
        Format = copy.Format,
        Location = location,
        HoldId = null
    };
}
