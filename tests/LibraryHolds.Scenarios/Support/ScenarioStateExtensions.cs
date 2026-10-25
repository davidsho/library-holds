namespace LibraryHolds.Scenarios.Support;

public static class ScenarioStateExtensions
{
    /// <summary>
    /// For context properties: a step used without the step it depends on fails by naming what's
    /// missing, not with a NullReferenceException.
    /// </summary>
    public static T OrNotYetGiven<T>(this T? value, string needed) where T : class =>
        value ?? throw new InvalidOperationException($"This step needs {needed}, but no earlier step in the scenario did that.");
}
