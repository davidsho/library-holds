// Drop-in from the reqnroll-scenarios skill (written for Reqnroll.xUnit 3.3.4).
// Replace the YourService namespace, and copy the feature files to the output directory (references/setup.md).
using Gherkin;
using Gherkin.CucumberMessages;
using Gherkin.CucumberMessages.Pickles;

using Io.Cucumber.Messages.Types;

using Reqnroll;
using Reqnroll.Bindings;

namespace YourService.Tests.Integration.Scenarios.Support;

/// <summary>
/// Fails the run when a step definition matches no line in any feature, so a step whose wording drifted away from
/// the Gherkin, or that its last feature stopped using, is deleted rather than left to rot. It reads every feature
/// file, not just the scenarios being run, so a filtered run reports the same as a full one, and a step used only by
/// an <c>@ignore</c> scenario still counts as used.
/// </summary>
[Binding]
public static class UnusedSteps
{
    private static IReadOnlyList<string>? _unused;

    /// <summary>
    /// Runs before the hooks that start each feature's app, so the run fails without starting containers. It is not
    /// a [BeforeTestRun], because that would also fail the plain xUnit tests in the same project.
    /// </summary>
    [BeforeFeature(Order = 0)]
    public static void RequireEveryStepDefinitionToBeUsed(IBindingRegistry bindings)
    {
        var unused = _unused ??= FindUnused(bindings);
        if (unused.Count > 0)
        {
            throw new InvalidOperationException(
                "These step definitions match no line in any feature. Delete them, or reuse their wording in the " +
                $"feature that was meant to use them:{Environment.NewLine}{string.Join(Environment.NewLine, unused)}");
        }
    }

    private static List<string> FindUnused(IBindingRegistry bindings)
    {
        var lines = FeatureLines().ToList();

        // [StepDefinition] registers one binding per keyword, so a method is used if any of them matches.
        return bindings.GetStepDefinitions()
            .GroupBy(step => $"{step.Method.Type.Name}.{step.Method.Name}")
            .Where(method => !method.Any(step => lines.Exists(line => Matches(step, line))))
            .Select(method => $"  {method.Key}: \"{method.First().SourceExpression}\"")
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static bool Matches(IStepDefinitionBinding step, PickleStep line) =>
        (line.Type == PickleStepType.UNKNOWN || KeywordOf(line.Type) == step.StepDefinitionType)
        && step.Regex.IsMatch(line.Text);

    private static StepDefinitionType KeywordOf(PickleStepType type) => type switch
    {
        PickleStepType.CONTEXT => StepDefinitionType.Given,
        PickleStepType.ACTION => StepDefinitionType.When,
        PickleStepType.OUTCOME => StepDefinitionType.Then,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Not a Given, When or Then.")
    };

    /// <summary>
    /// Every step line with outline placeholders filled in from each Examples row, and And/But resolved to the
    /// keyword they continue, which is what Reqnroll itself matches against.
    /// </summary>
    private static IEnumerable<PickleStep> FeatureLines()
    {
        var ids = new IncrementingIdGenerator();
        var features = Directory.GetFiles(AppContext.BaseDirectory, "*.feature", SearchOption.AllDirectories);
        if (features.Length == 0)
        {
            throw new InvalidOperationException(
                $"No .feature files were found under {AppContext.BaseDirectory}. Copy them to the output directory " +
                "in the test .csproj so the unused-step check can read them.");
        }

        return features
            .Select(path => new AstMessagesConverter(ids).ConvertGherkinDocumentToEventArgs(new Parser().Parse(path), path))
            .SelectMany(document => new PickleCompiler(ids).Compile(document))
            .SelectMany(pickle => pickle.Steps);
    }
}
