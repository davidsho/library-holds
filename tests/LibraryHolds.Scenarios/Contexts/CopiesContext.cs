using LibraryHolds.Scenarios.Model;
using LibraryHolds.Scenarios.Support;

namespace LibraryHolds.Scenarios.Contexts;

/// <summary>The copies returned so far in this scenario, in the order they were returned.</summary>
public sealed class CopiesContext
{
    private readonly List<ReturnedCopy> _returned = [];

    public void Add(ReturnedCopy copy) => _returned.Add(copy);

    public ReturnedCopy Latest => _returned.LastOrDefault().OrNotYetGiven("a copy to have been returned");

    public ReturnedCopy ReturnedAt(DateTimeOffset at) =>
        _returned.SingleOrDefault(c => c.ReturnedAt == at).OrNotYetGiven($"a copy to have been returned at {Wire.Instant(at)}");
}
