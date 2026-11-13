// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
namespace YourService.Tests.Integration.Scenarios.Contexts;

internal static class ScenarioStateExtensions
{
    /// <summary>
    /// The state an earlier step set, or an error naming the missing step, rather than a
    /// <see cref="NullReferenceException"/> later on.
    /// </summary>
    public static T OrNotYetGiven<T>(this T? state, string whatTheStepNeeds) where T : class =>
        state ?? throw new InvalidOperationException(
            $"This step needs {whatTheStepNeeds}, but no earlier step in the scenario did that.");
}
