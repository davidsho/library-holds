using LibraryHolds.Scenarios.Support.App;
using Reqnroll;

namespace LibraryHolds.Scenarios.Support;

[Binding]
public sealed class Hooks(LibraryHoldsApp app)
{
    [BeforeFeature("HoldsForReturnedCopies")]
    public static Task StartTheAppAsync(FeatureContext feature) => feature.StartAppAsync();

    [BeforeFeature(Order = 20_000)]
    public static void RequireAnAppForTheFeature(FeatureContext feature)
    {
        if (!feature.FeatureContainer.IsRegistered<LibraryHoldsApp>())
        {
            throw new InvalidOperationException(
                $"Feature '{feature.FeatureInfo.Title}' has no app to run against. Add a [BeforeFeature] for its tag in Hooks.cs.");
        }
    }

    [AfterFeature]
    public static Task StopTheAppAsync(FeatureContext feature) => feature.StopAppAsync();

    [BeforeScenario]
    public Task ForgetEarlierScenariosAsync() => app.ForgetEarlierScenariosAsync();
}
