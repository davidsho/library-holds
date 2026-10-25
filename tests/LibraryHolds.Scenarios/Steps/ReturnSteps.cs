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
    [Given("a copy of {string} in {} is returned at {word}")]
    [When("a copy of {string} in {} is returned at {word}")]
    public Task ACopyIsReturnedAsync(string title, string format, string at) =>
        ReturnAsync(ReturnsDriver.NewCopy(title, Wire.Format(format), Wire.ParseInstant(at)));

    /// <summary>
    /// The switch is the one list of what the matching rule depends on. Adding a detail is one more
    /// Examples row and one more arm.
    /// </summary>
    [When(@"^a copy is returned at (\S+) that matches (\S+)'s hold except for its (\w+), which is (.+)$")]
    public Task WhenACopyIsReturnedThatMatchesMembersHoldExceptForOneDetailAsync(string at, string member, string detail, string value)
    {
        var hold = placed.For(member);
        var matching = ReturnsDriver.NewCopy(hold.Title, hold.Format, Wire.ParseInstant(at));
        return ReturnAsync(detail switch
        {
            "title" => matching with { Title = value },
            "format" => matching with { Format = Wire.Format(value) },
            _ => throw new ArgumentOutOfRangeException(nameof(detail), detail, "Not a detail the matching rule depends on.")
        });
    }

    [When("a copy of that title in that format is returned at a random time")]
    public Task WhenACopyOfThatTitleInThatFormatIsReturnedAtARandomTimeAsync() =>
        ReturnAsync(ReturnsDriver.NewCopy(placed.First.Title, placed.First.Format, randomInstants.Return("at a random time")));

    [When("a copy matching {word}'s hold is returned {}")]
    public Task WhenACopyMatchingMembersHoldIsReturnedAsync(string member, string when)
    {
        var hold = placed.For(member);
        return ReturnAsync(ReturnsDriver.NewCopy(hold.Title, hold.Format, randomInstants.Return(when)));
    }

    [Then("the copy is on the hold shelf for {word}")]
    public Task ThenTheCopyIsOnTheHoldShelfForAsync(string member) =>
        returns.AssertOnTheHoldShelfForAsync(returned.Latest, placed.For(member));

    [Then("the copy is back on the shelf")]
    public Task ThenTheCopyIsBackOnTheShelfAsync() => returns.AssertBackOnTheShelfAsync(returned.Latest);

    private async Task ReturnAsync(ReturnedCopy copy)
    {
        await returns.ReturnAsync(copy);
        returned.Add(copy);
    }
}
