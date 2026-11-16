using LibraryHolds.Scenarios.Contexts;
using LibraryHolds.Scenarios.Drivers;
using LibraryHolds.Scenarios.Model;
using LibraryHolds.Scenarios.Support;
using LibraryHolds.Scenarios.Support.Randomised;
using Reqnroll;

namespace LibraryHolds.Scenarios.Steps;

[Binding]
public sealed class ReturnSteps(ReturnsDriver returns, CopiesContext returned, HoldsContext placed, RandomInstants randomInstants)
{
    /// <summary>Every copy these steps return is a new copy, with a barcode no earlier step used.</summary>
    [Given(@"^a copy of ""([^""]+)"" in ([a-z ]+) was returned at (\S+)$")]
    [When(@"^(?:a|another) copy of ""([^""]+)"" in ([a-z ]+) is returned at (\S+)$")]
    public Task WhenACopyOfTitleInFormatIsReturnedAtAsync(string title, string format, string at) =>
        ReturnAsync(ReturnsDriver.NewCopy(title, Wire.Format(format), Wire.ParseInstant(at)));

    /// <summary>
    /// The random hold steps put every hold in the scenario on one title in one format, so the first hold's
    /// title and format are "that title" and "that format".
    /// </summary>
    [When(@"^a copy of that title in that format is returned (.+)$")]
    public Task WhenACopyOfThatTitleInThatFormatIsReturnedAsync(string when) =>
        ReturnAsync(ReturnsDriver.NewCopy(placed.First.Title, placed.First.Format, randomInstants.Return(when)));

    [Then("the copy is on the hold shelf for {word}")]
    public Task ThenTheCopyIsOnTheHoldShelfForAsync(string member) =>
        returns.AssertOnTheHoldShelfForAsync(returned.Latest, placed.For(member));

    [Then("the copy is back on the shelf")]
    public Task ThenTheCopyIsBackOnTheShelfAsync() => returns.AssertBackOnTheShelfAsync(returned.Latest);

    private Task ReturnAsync(ReturnedCopy copy)
    {
        returned.Add(copy);
        return returns.ReturnAsync(copy);
    }
}
