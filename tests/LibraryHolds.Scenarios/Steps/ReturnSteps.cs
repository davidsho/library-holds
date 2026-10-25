using LibraryHolds.Scenarios.Contexts;
using LibraryHolds.Scenarios.Drivers;
using LibraryHolds.Scenarios.Model;
using LibraryHolds.Scenarios.Support;
using LibraryHolds.Scenarios.Support.Randomised;
using Reqnroll;

namespace LibraryHolds.Scenarios.Steps;

/// <summary>Returning copies, and where a returned copy ends up.</summary>
[Binding]
public sealed class ReturnSteps(
    ReturnsDriver returns,
    HoldsDriver holds,
    CopiesContext returned,
    HoldsContext placed,
    RandomInstants randomInstants)
{
    // Regex rather than a Cucumber expression, because "large print" is two words.
    [When(@"^an? (hardback|paperback|large print|audiobook) copy of ""([^""]*)"" is returned at (\S+)$")]
    public Task WhenACopyOfTitleIsReturnedAtAsync(string format, string title, string at) =>
        ReturnAsync(ReturnsDriver.NewCopy(title, Wire.Format(format), Wire.ParseInstant(at)));

    [Given(@"^an? (hardback|paperback|large print|audiobook) copy of ""([^""]*)"" was returned at (\S+) and filled (\w+)'s hold$")]
    public async Task GivenACopyOfTitleWasReturnedAtAndFilledHoldAsync(string format, string title, string at, string member)
    {
        await WhenACopyOfTitleIsReturnedAtAsync(format, title, at);
        await holds.RequireReadyForCollectionAsync(placed.For(member), returned.Latest);
    }

    /// <summary>The mode aims the random return time at the ranges the collect-by rule finds hardest.</summary>
    [When(@"^a copy of that title in that format is returned (at a random time|in the week before a clock change|around midnight UK time)$")]
    public Task WhenACopyOfThatTitleInThatFormatIsReturnedAsync(string when) =>
        ReturnAsync(ReturnsDriver.NewCopy(placed.First.Title, placed.First.Format, randomInstants.Return(when)));

    [Then("the copy is on the hold shelf for {word}")]
    public Task ThenTheCopyIsOnTheHoldShelfForAsync(string member) =>
        returns.AssertOnTheHoldShelfForAsync(returned.Latest, placed.For(member));

    [Then("the copy is on the hold shelf for the member who placed the first hold")]
    public Task ThenTheCopyIsOnTheHoldShelfForTheMemberWhoPlacedTheFirstHoldAsync() =>
        returns.AssertOnTheHoldShelfForAsync(returned.Latest, placed.First);

    [Then("the copy is back on the shelf")]
    public Task ThenTheCopyIsBackOnTheShelfAsync() => returns.AssertBackOnTheShelfAsync(returned.Latest);

    private async Task ReturnAsync(ReturnedCopy copy)
    {
        await returns.ReturnAsync(copy);
        returned.Add(copy);
    }
}
