using LibraryHolds.Scenarios.Contexts;
using LibraryHolds.Scenarios.Drivers;
using Reqnroll;

namespace LibraryHolds.Scenarios.Steps;

/// <summary>What the notification provider is asked to tell members.</summary>
[Binding]
public sealed class NotificationSteps(NotificationsDriver notifications, HoldsContext placed, CopiesContext returned)
{
    [Then("{word} is told the copy is ready")]
    public Task ThenMemberIsToldTheCopyIsReadyAsync(string member) =>
        notifications.AssertToldTheCopyIsReadyAsync(placed.For(member), returned.Latest);

    [Then("the first member is told the copy is ready")]
    public Task ThenTheFirstMemberIsToldTheCopyIsReadyAsync() =>
        notifications.AssertToldTheCopyIsReadyAsync(placed.First, returned.Latest);

    [Then("{word} is not told anything")]
    public Task ThenMemberIsNotToldAnythingAsync(string member) => notifications.AssertNotToldAnythingAsync(member);

    [Then("nobody is told anything")]
    public Task ThenNobodyIsToldAnythingAsync() => notifications.AssertNobodyToldAnythingAsync();
}
