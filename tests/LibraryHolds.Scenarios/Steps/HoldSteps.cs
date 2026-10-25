using LibraryHolds.Scenarios.Contexts;
using LibraryHolds.Scenarios.Drivers;
using LibraryHolds.Scenarios.Support;
using LibraryHolds.Scenarios.Support.Randomised;
using Reqnroll;

namespace LibraryHolds.Scenarios.Steps;

/// <summary>Placing and cancelling holds, and what state a hold is in afterwards.</summary>
[Binding]
public sealed class HoldSteps(
    HoldsDriver holds,
    HoldsContext placed,
    CopiesContext returned,
    RandomHoldDetails randomDetails,
    RandomInstants randomInstants)
{
    // Regex rather than a Cucumber expression, because "large print" is two words.
    [Given(@"^(\w+) placed a hold on the (hardback|paperback|large print|audiobook) of ""([^""]*)"" at (\S+)$")]
    public async Task GivenPlacedAHoldOnTheFormatOfTitleAtAsync(string member, string format, string title, string at) =>
        placed.Add(await holds.PlaceAsync(member, title, Wire.Format(format), Wire.ParseInstant(at)));

    [Given("{word}'s hold was cancelled")]
    public Task GivenHoldWasCancelledAsync(string member) => holds.CancelAsync(placed.For(member));

    [Given("a random member placed a hold on a random title in a random format")]
    public Task GivenARandomMemberPlacedAHoldOnARandomTitleInARandomFormatAsync() => PlaceRandomHoldsAsync(1);

    [Given("{int} random members placed holds on the same random title in the same random format, one after another")]
    public Task GivenRandomMembersPlacedHoldsOnTheSameRandomTitleInTheSameRandomFormatOneAfterAnotherAsync(int count) =>
        PlaceRandomHoldsAsync(count);

    [Then("{word}'s hold is ready for collection with the copy until {word}")]
    public Task ThenHoldIsReadyForCollectionWithTheCopyUntilAsync(string member, string collectBy) =>
        holds.AssertReadyForCollectionAsync(placed.For(member), returned.Latest, collectBy);

    [Then("{word}'s hold is still ready for collection with the copy returned at {word} until {word}")]
    public Task ThenHoldIsStillReadyForCollectionWithTheCopyReturnedAtUntilAsync(string member, string returnedAt, string collectBy) =>
        holds.AssertReadyForCollectionAsync(placed.For(member), returned.ReturnedAt(Wire.ParseInstant(returnedAt)), collectBy);

    /// <summary>The expected time is worked out with NodaTime, independently of the API's own calculation.</summary>
    [Then("that member's hold is ready for collection with the copy until 20:00 UK time on the seventh day after it was returned")]
    public Task ThenThatMembersHoldIsReadyForCollectionUntilTheSeventhDayAfterItWasReturnedAsync() =>
        holds.AssertReadyForCollectionAsync(placed.First, returned.Latest, Wire.Instant(ExpectedCollectBy.For(returned.Latest.ReturnedAt)));

    [Then("{word}'s hold is still waiting")]
    public Task ThenHoldIsStillWaitingAsync(string member) => holds.AssertWaitingAsync(placed.For(member));

    [Then("{word}'s hold is still cancelled")]
    public Task ThenHoldIsStillCancelledAsync(string member) => holds.AssertCancelledAsync(placed.For(member));

    [Then("every other member's hold is still waiting")]
    public async Task ThenEveryOtherMembersHoldIsStillWaitingAsync()
    {
        foreach (var hold in placed.AllButTheFirst)
        {
            await holds.AssertWaitingAsync(hold);
        }
    }

    /// <summary>Distinct members, one title and one format, placed in order, so the first hold placed has waited longest.</summary>
    private async Task PlaceRandomHoldsAsync(int count)
    {
        var title = randomDetails.Title();
        var format = Wire.Format(randomDetails.Format());
        var members = randomDetails.Members(count);
        var instants = randomInstants.HoldsPlacedOneAfterAnother(count);
        for (var i = 0; i < count; i++)
        {
            placed.Add(await holds.PlaceAsync(members[i], title, format, instants[i]));
        }
    }
}
