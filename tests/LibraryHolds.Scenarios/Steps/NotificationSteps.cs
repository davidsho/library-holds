using LibraryHolds.Scenarios.Contexts;
using LibraryHolds.Scenarios.Drivers;
using Reqnroll;

namespace LibraryHolds.Scenarios.Steps;

[Binding]
public sealed class NotificationSteps(NotificationsDriver notifications, HoldsContext placed, CopiesContext returned)
{
    [Then("{word} is told the copy is ready")]
    public Task ThenMemberIsToldTheCopyIsReadyAsync(string member) =>
        notifications.AssertToldTheCopyIsReadyAsync(placed.For(member), returned.Latest);

    [Then("the member who placed the first hold is told the copy is ready")]
    public Task ThenTheMemberWhoPlacedTheFirstHoldIsToldTheCopyIsReadyAsync() =>
        notifications.AssertToldTheCopyIsReadyAsync(placed.First, returned.Latest);

    [Then("nobody is told anything")]
    public Task ThenNobodyIsToldAnythingAsync() => notifications.AssertNobodyToldAnythingAsync();
}
