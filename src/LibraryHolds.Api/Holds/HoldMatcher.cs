using LibraryHolds.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryHolds.Api.Holds;

public interface IHoldRepository
{
    Task<IReadOnlyList<Hold>> FindWaitingHoldsAsync(string title, Format format, CancellationToken cancellationToken);
}

public sealed class HoldRepository(LibraryDbContext db) : IHoldRepository
{
    public async Task<IReadOnlyList<Hold>> FindWaitingHoldsAsync(string title, Format format, CancellationToken cancellationToken) =>
        await db.Holds
            .Where(h => h.Status == HoldStatus.Waiting && h.Title == title)
            .ToListAsync(cancellationToken);
}

/// <summary>Decides which waiting hold, if any, a returned copy fills.</summary>
public sealed class HoldMatcher(IHoldRepository holds)
{
    public async Task<Hold?> FindHoldToFillAsync(string title, Format format, CancellationToken cancellationToken)
    {
        var waiting = await holds.FindWaitingHoldsAsync(title, format, cancellationToken);
        return waiting.MinBy(h => h.PlacedAt);
    }
}
