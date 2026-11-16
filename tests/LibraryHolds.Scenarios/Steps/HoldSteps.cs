using LibraryHolds.Scenarios.Contexts;
using LibraryHolds.Scenarios.Drivers;
using LibraryHolds.Scenarios.Support;
using LibraryHolds.Scenarios.Support.Randomised;
using Reqnroll;

namespace LibraryHolds.Scenarios.Steps;

[Binding]
public sealed class HoldSteps(
    HoldsDriver holds,
    HoldsContext placed,
    CopiesContext returned,
    RandomHoldDetails randomDetails,
    RandomInstants randomInstants)
{
    // The format is captured as words ("large print"); Wire.Format is the one list of formats the library lends.
    [Given(@"^(\w+) placed a hold on ""([^""]+)"" in ([a-z ]+) at (\S+)$")]
    public async Task GivenMemberPlacedAHoldOnTitleInFormatAtAsync(string member, string title, string format, string at) =>
        placed.Add(await holds.PlaceAsync(member, title, Wire.Format(format), Wire.ParseInstant(at)));

    [Given(@"^(\w+) placed a hold on a random title in a random format$")]
    public async Task GivenMemberPlacedAHoldOnARandomTitleInARandomFormatAsync(string member) =>
        placed.Add(await holds.PlaceAsync(
            member,
            randomDetails.Title(),
            Wire.Format(randomDetails.Format()),
            randomInstants.HoldsPlacedOneAfterAnother(1)[0]));

    /// <summary>Every hold is on the same title in the same format, placed in the order the members are listed.</summary>
    [Given("{int} members placed holds on a random title in a random format, one after another")]
    public async Task GivenMembersPlacedHoldsOnARandomTitleInARandomFormatOneAfterAnotherAsync(int count)
    {
        var title = randomDetails.Title();
        var format = Wire.Format(randomDetails.Format());
        foreach (var (member, at) in randomDetails.Members(count).Zip(randomInstants.HoldsPlacedOneAfterAnother(count)))
        {
            placed.Add(await holds.PlaceAsync(member, title, format, at));
        }
    }

    [Given("{word}'s hold was cancelled")]
    public Task GivenMembersHoldWasCancelledAsync(string member) => holds.CancelAsync(placed.For(member));

    [Given("{word}'s hold is ready for collection")]
    public Task GivenMembersHoldIsReadyForCollectionAsync(string member) =>
        holds.RequireReadyForCollectionAsync(placed.For(member), returned.Latest);

    [Then("{word}'s hold is ready for collection")]
    public Task ThenMembersHoldIsReadyForCollectionAsync(string member) =>
        holds.AssertReadyForCollectionAsync(placed.For(member), returned.Latest);

    /// <summary>The collect-by time is compared as the text the API sends, so its UK offset is checked too.</summary>
    [Then("{word}'s hold is ready for collection until {word}")]
    public Task ThenMembersHoldIsReadyForCollectionUntilAsync(string member, string collectBy) =>
        holds.AssertReadyForCollectionAsync(placed.For(member), returned.Latest, collectBy);

    [Then("{word}'s hold is still ready for collection with the copy returned at {word}")]
    public Task ThenMembersHoldIsStillReadyForCollectionWithTheCopyReturnedAtAsync(string member, string at) =>
        holds.AssertReadyForCollectionAsync(placed.For(member), returned.ReturnedAt(Wire.ParseInstant(at)));

    [Then("the first member's hold is ready for collection")]
    public Task ThenTheFirstMembersHoldIsReadyForCollectionAsync() =>
        holds.AssertReadyForCollectionAsync(placed.First, returned.Latest);

    [Then("{word}'s hold is still waiting")]
    public Task ThenMembersHoldIsStillWaitingAsync(string member) => holds.AssertWaitingAsync(placed.For(member));

    [Then("every other member's hold is still waiting")]
    public async Task ThenEveryOtherMembersHoldIsStillWaitingAsync()
    {
        foreach (var hold in placed.AllButTheFirst)
        {
            await holds.AssertWaitingAsync(hold);
        }
    }

    [Then("{word}'s hold is still cancelled")]
    public Task ThenMembersHoldIsStillCancelledAsync(string member) => holds.AssertCancelledAsync(placed.For(member));
}
