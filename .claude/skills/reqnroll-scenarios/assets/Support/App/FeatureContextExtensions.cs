// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
using Reqnroll;

namespace YourService.Tests.Integration.Scenarios.Support.App;

/// <summary>
/// Starts a feature's <see cref="AppUnderTest"/> once, before its first scenario, and stops it after
/// the last. Starting the containers takes a while, so every scenario in the feature shares the one app.
/// <para>
/// It is registered as <see cref="AppUnderTest"/>, not as the feature's own type, so every driver
/// depends on the base type and works in any feature.
/// </para>
/// </summary>
internal static class FeatureContextExtensions
{
    public static async Task StartAppAsync<TApp>(this FeatureContext feature) where TApp : AppUnderTest, new() =>
        feature.FeatureContainer.RegisterInstanceAs<AppUnderTest>(await AppUnderTest.StartNewAsync<TApp>());

    public static bool HasApp(this FeatureContext feature) => feature.FeatureContainer.IsRegistered<AppUnderTest>();

    /// <summary>
    /// Does nothing if the app never started: <see cref="AppUnderTest.StartNewAsync{TApp}"/> has
    /// already cleaned up after itself, and a second error here would only bury the first.
    /// </summary>
    public static async Task StopAppAsync(this FeatureContext feature)
    {
        if (feature.HasApp())
        {
            await feature.FeatureContainer.Resolve<AppUnderTest>().DisposeAsync();
        }
    }
}
