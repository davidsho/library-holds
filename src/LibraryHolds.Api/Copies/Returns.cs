using LibraryHolds.Api.Data;
using LibraryHolds.Api.Holds;
using LibraryHolds.Api.Notifications;
using LibraryHolds.Api.Time;

namespace LibraryHolds.Api.Copies;

/// <summary>
/// A returned copy either fills the hold of the member who has waited longest for it, or goes back on
/// the shelf. The member's notification is written to the outbox in the same transaction, and sent
/// to the notification provider afterwards.
/// </summary>
public sealed class Returns(LibraryDbContext db, HoldMatcher matcher, IClock clock)
{
    public async Task<Copy> ReceiveAsync(Guid copyId, string title, Format format, CancellationToken cancellationToken)
    {
        var returnedAt = clock.UtcNow;
        var copy = await db.Copies.FindAsync([copyId], cancellationToken);
        if (copy is null)
        {
            copy = new Copy { Id = copyId, Title = title, Format = format };
            db.Copies.Add(copy);
        }

        var hold = await matcher.FindHoldToFillAsync(copy.Title, copy.Format, cancellationToken);
        if (hold is null)
        {
            copy.PutOnShelf(returnedAt);
        }
        else
        {
            var collectBy = CollectBy.For(returnedAt);
            hold.FillWith(copy.Id, collectBy);
            copy.PutOnHoldShelfFor(hold, returnedAt);
            db.Outbox.Add(OutboxMessage.For(new HoldReadyNotification(hold.MemberId, hold.Id, hold.Title, hold.Format, collectBy)));
        }

        await db.SaveChangesAsync(cancellationToken);
        return copy;
    }
}
