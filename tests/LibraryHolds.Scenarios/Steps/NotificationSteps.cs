using LibraryHolds.Scenarios.Contexts;
using LibraryHolds.Scenarios.Drivers;
using Reqnroll;

namespace LibraryHolds.Scenarios.Steps;

/// <summary>What the notification provider was asked to tell members.</summary>
[Binding]
public sealed class NotificationSteps(NotificationsDriver notifications, HoldsContext placed, CopiesContext returned)
{
    [Then("{word} is told the copy is ready")]
    public Task ThenIsToldTheCopyIsReadyAsync(string member) =>
        notifications.AssertToldTheCopyIsReadyAsync(placed.For(member), returned.Latest);

    [Then("that member is told the copy is ready")]
    public Task ThenThatMemberIsToldTheCopyIsReadyAsync() =>
        notifications.AssertToldTheCopyIsReadyAsync(placed.First, returned.Latest);

    [Then("{word} is not told anything")]
    public Task ThenIsNotToldAnythingAsync(string member) => notifications.AssertNotToldAnythingAsync(placed.For(member));

    [Then("no other member is told anything")]
    public async Task ThenNoOtherMemberIsToldAnythingAsync()
    {
        foreach (var hold in placed.AllButTheFirst)
        {
            await notifications.AssertNotToldAnythingAsync(hold);
        }
    }

    [Then("nobody is told anything")]
    public Task ThenNobodyIsToldAnythingAsync() => notifications.AssertNobodyToldAnythingAsync();
}
