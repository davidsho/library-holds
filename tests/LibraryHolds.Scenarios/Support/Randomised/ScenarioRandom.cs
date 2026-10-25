using System.Globalization;
using Bogus;
using Reqnroll;

namespace LibraryHolds.Scenarios.Support.Randomised;

/// <summary>
/// One seeded source of randomness per scenario. The seed is logged the first time a scenario uses it,
/// and setting SCENARIO_SEED to it replays the run exactly.
/// </summary>
public sealed class ScenarioRandom(IReqnrollOutputHelper output)
{
    public const string SeedVariable = "SCENARIO_SEED";

    private Randomizer? _randomizer;

    public Randomizer Randomizer => _randomizer ??= Seeded();

    private Randomizer Seeded()
    {
        var seed = ReadSeed();
        output.WriteLine($"Random seed: {seed} (set {SeedVariable}={seed} to replay)");
        return new Randomizer(seed);
    }

    /// <summary>A seed that isn't a whole number fails the scenario, so a replay never quietly runs a different seed.</summary>
    private static int ReadSeed()
    {
        var text = Environment.GetEnvironmentVariable(SeedVariable);
        if (text is null)
        {
            return Random.Shared.Next();
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var seed)
            ? seed
            : throw new InvalidOperationException($"{SeedVariable} is '{text}', which isn't a whole number, so this run can't replay it.");
    }
}
