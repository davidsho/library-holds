using System.Globalization;
using FluentAssertions;
using LibraryHolds.Api.Holds;

namespace LibraryHolds.Tests.Unit;

/// <summary>
/// Pure date arithmetic with many edge cases is where a plain xUnit theory beats a scenario: every
/// row below runs in microseconds, with no app, database or clock to set up.
/// </summary>
public sealed class CollectByTests
{
    [Theory]
    [InlineData("2027-01-12T14:00:00Z", "2027-01-19T20:00:00+00:00")] // winter
    [InlineData("2027-06-15T14:00:00Z", "2027-06-22T20:00:00+01:00")] // summer
    [InlineData("2027-01-12T21:30:00Z", "2027-01-19T20:00:00+00:00")] // after closing time, same day
    [InlineData("2027-01-12T23:59:59Z", "2027-01-19T20:00:00+00:00")] // last second of a winter day
    [InlineData("2027-06-15T22:59:59Z", "2027-06-22T20:00:00+01:00")] // last second of a UK summer day
    [InlineData("2027-06-15T23:00:00Z", "2027-06-23T20:00:00+01:00")] // first second of the next UK day
    [InlineData("2027-03-21T14:00:00Z", "2027-03-28T20:00:00+01:00")] // collect-by day is the day the clocks go forward
    [InlineData("2027-03-28T00:30:00Z", "2027-04-04T20:00:00+01:00")] // returned just before the clocks go forward
    [InlineData("2027-10-24T13:00:00Z", "2027-10-31T20:00:00+00:00")] // collect-by day is the day the clocks go back
    [InlineData("2027-10-31T00:30:00Z", "2027-11-07T20:00:00+00:00")] // returned in the hour that happens twice
    [InlineData("2027-10-31T01:30:00Z", "2027-11-07T20:00:00+00:00")] // ...and in its second run
    [InlineData("2027-12-28T10:00:00Z", "2028-01-04T20:00:00+00:00")] // across the new year
    [InlineData("2028-02-22T10:00:00Z", "2028-02-29T20:00:00+00:00")] // into a leap day
    public void Is_closing_time_on_the_seventh_day_after_the_copy_reaches_the_hold_shelf(string reachedHoldShelf, string expected)
    {
        var reached = DateTimeOffset.ParseExact(reachedHoldShelf, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        var collectBy = CollectBy.For(reached);

        // Compared as text so the UK offset is checked too, not only the instant.
        collectBy.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture).Should().Be(expected);
    }
}
