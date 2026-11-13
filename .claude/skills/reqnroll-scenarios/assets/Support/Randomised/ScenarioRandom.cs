// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
using System.Globalization;

using Bogus;

using Reqnroll;

namespace YourService.Tests.Integration.Scenarios.Support.Randomised;

/// <summary>
/// The source of randomness for <c>@Randomised</c> scenarios. Reqnroll constructs one instance per
/// scenario and injects it into each feature's generator, so all of a scenario's random values come from
/// one seed. The seed is logged, and setting <c>SCENARIO_SEED</c> to it replays the failure (see README.md).
/// </summary>
public sealed class ScenarioRandom
{
    private const string SeedVariable = "SCENARIO_SEED";

    /// <summary>£1m in pence: comfortably inside what the webhooks carry, and always a whole number of pence.</summary>
    private const int LargestAmountInPence = 100_000_000;

    public ScenarioRandom(IReqnrollOutputHelper output)
    {
        var seed = SeedRequestedByTheEnvironment() ?? Random.Shared.Next();

        output.WriteLine(
            string.Create(CultureInfo.InvariantCulture, $"Random seed: {seed} (set {SeedVariable}={seed} to replay)"));
        Randomizer = new Randomizer(seed);
    }

    public Randomizer Randomizer { get; }

    public decimal Amount() => Randomizer.Int(1, LargestAmountInPence) / 100m;

    /// <summary>
    /// A seed that cannot be read fails the scenario, rather than quietly running with a random one and
    /// leaving whoever is replaying a failure to wonder why it no longer reproduces.
    /// </summary>
    private static int? SeedRequestedByTheEnvironment()
    {
        var requested = Environment.GetEnvironmentVariable(SeedVariable);

        if (string.IsNullOrWhiteSpace(requested))
        {
            return null;
        }

        return int.TryParse(requested, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed)
            ? seed
            : throw new InvalidOperationException($"{SeedVariable} is '{requested}', which is not a whole number.");
    }
}
