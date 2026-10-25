using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibraryHolds.Scenarios.Support;

/// <summary>
/// Reads the API's JSON into the contracts in Model/Contracts.cs, which are spelt out here rather than
/// borrowed from the API project. A field the API adds fails the read, because unmapped members are
/// disallowed; a field it drops fails it, because every contract member is required; and a field it
/// changes fails the equivalence assertion that follows. A contract can't quietly move with the code.
/// </summary>
public static class StrictJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static T Parse<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options)
                ?? throw new InvalidOperationException($"Expected a {typeof(T).Name}, but the JSON was null.");
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException(
                $"The JSON no longer matches the {typeof(T).Name} contract the scenarios spell out: {e.Message}{Environment.NewLine}{json}", e);
        }
    }

    public static async Task<T> ReadStrictJsonAsync<T>(this HttpResponseMessage response) =>
        Parse<T>(await response.Content.ReadAsStringAsync());

    public static async Task<T> GetFromStrictJsonAsync<T>(this HttpClient api, string path)
    {
        using var response = await api.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.ReadStrictJsonAsync<T>();
    }
}

/// <summary>The exact text formats the scenarios and the API agree on.</summary>
public static class Wire
{
    private const string UtcInstant = "yyyy-MM-dd'T'HH:mm:ss'Z'";
    private const string InstantWithOffset = "yyyy-MM-dd'T'HH:mm:sszzz";

    /// <summary>An instant as the Gherkin writes it, always in UTC: 2027-03-01T09:00:00Z.</summary>
    public static DateTimeOffset ParseInstant(string text) =>
        DateTimeOffset.ParseExact(text, UtcInstant, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    /// <summary>An instant as the API sends it, with its offset: 2027-03-28T20:00:00+01:00.</summary>
    public static string Instant(DateTimeOffset value) => value.ToString(InstantWithOffset, CultureInfo.InvariantCulture);

    /// <summary>A format as the Gherkin says it ("large print"), as the API names it ("LargePrint").</summary>
    public static string Format(string words) => words switch
    {
        "hardback" => "Hardback",
        "paperback" => "Paperback",
        "large print" => "LargePrint",
        "audiobook" => "Audiobook",
        _ => throw new ArgumentOutOfRangeException(nameof(words), words, "Not a format the library lends.")
    };
}
