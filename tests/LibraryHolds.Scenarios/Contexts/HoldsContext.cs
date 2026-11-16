using LibraryHolds.Scenarios.Model;
using LibraryHolds.Scenarios.Support;

namespace LibraryHolds.Scenarios.Contexts;

/// <summary>The holds placed so far in this scenario, in the order they were placed.</summary>
public sealed class HoldsContext
{
    private readonly List<PlacedHold> _inOrderPlaced = [];

    public void Add(PlacedHold hold) => _inOrderPlaced.Add(hold);

    public PlacedHold For(string member) =>
        _inOrderPlaced.SingleOrDefault(h => h.Member == member).OrNotYetGiven($"{member} to have placed a hold");

    public PlacedHold First => _inOrderPlaced.FirstOrDefault().OrNotYetGiven("a hold to have been placed");

    public IReadOnlyList<PlacedHold> AllButTheFirst => _inOrderPlaced.Skip(1).ToList();
}
