using System.Globalization;
using Bogus.DataSets;

namespace LibraryHolds.Scenarios.Support.Randomised;

/// <summary>Random but valid members, titles and formats.</summary>
public sealed class RandomHoldDetails(ScenarioRandom random)
{
    private static readonly string[] FormatWords = ["hardback", "paperback", "large print", "audiobook"];

    /// <summary>Distinct members, so no two holds in a scenario belong to the same person by accident.</summary>
    public IReadOnlyList<string> Members(int count)
    {
        var names = new Name { Random = random.Randomizer };
        return Enumerable.Range(1, count).Select(n => $"{names.FirstName()}{n}").ToList();
    }

    /// <summary>An obviously fictitious title, such as "The Dolor of Amet".</summary>
    public string Title()
    {
        var words = new Lorem { Random = random.Randomizer };
        return $"The {Capitalised(words.Word())} of {Capitalised(words.Word())}";
    }

    /// <summary>A format as the Gherkin would say it.</summary>
    public string Format() => random.Randomizer.ArrayElement(FormatWords);

    private static string Capitalised(string word) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word);
}
