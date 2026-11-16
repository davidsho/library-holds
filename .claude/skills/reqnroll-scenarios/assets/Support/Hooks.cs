// Template from the reqnroll-scenarios skill.
// One [BeforeFeature] per feature tag; keep the guard and the AfterFeature as they are.
using Reqnroll;

using YourService.Tests.Integration.Scenarios.Support.App;

namespace YourService.Tests.Integration.Scenarios.Support;

/// <summary>
/// Which app each feature runs against, chosen by the feature's tag, and what a feature resets between scenarios.
/// </summary>
[Binding]
public sealed class Hooks(/* drivers needed by the BeforeScenario resets */)
{
    [BeforeFeature("<FeatureTag>")]
    public static Task StartThe<FeatureTag>AppAsync(FeatureContext feature) =>
        feature.StartAppAsync<<FeatureTag>App>();

    /// <summary>
    /// Runs after the hooks above, so a feature whose tag none of them handles fails here, by name, rather than
    /// at its first step with a container error.
    /// </summary>
    [BeforeFeature(Order = 20_000)]
    public static void RequireAnAppForTheFeature(FeatureContext feature)
    {
        if (!feature.HasApp())
        {
            throw new InvalidOperationException(
                $"Feature '{feature.FeatureInfo.Title}' has no app to run against. Give it a tag, and start the app " +
                $"it needs in a [BeforeFeature] hook for that tag in {nameof(Hooks)}.cs.");
        }
    }

    [AfterFeature]
    public static Task StopTheAppAsync(FeatureContext feature) => feature.StopAppAsync();

    // Only where a feature's scenarios could see each other's data. A reset waits for the app to settle first,
    // so nothing an earlier scenario set off lands after it.
    // [BeforeScenario("<FeatureTag>")]
    // public Task ForgetEarlierScenarios<Things>Async() => <driver>.ForgetEarlierScenarios<Things>Async();
}
