using Reqnroll;

namespace LibraryHolds.Scenarios.Support.App;

public static class FeatureContextExtensions
{
    /// <summary>Starts the app and makes it available to every driver for the rest of the feature.</summary>
    public static async Task StartAppAsync(this FeatureContext feature)
    {
        var app = new LibraryHoldsApp();
        feature.FeatureContainer.RegisterInstanceAs(app);
        await app.StartAsync();
    }

    /// <summary>Does nothing if the app never started, so a start-up failure isn't hidden behind a second error.</summary>
    public static async Task StopAppAsync(this FeatureContext feature)
    {
        if (feature.FeatureContainer.IsRegistered<LibraryHoldsApp>())
        {
            await feature.FeatureContainer.Resolve<LibraryHoldsApp>().DisposeAsync();
        }
    }
}
