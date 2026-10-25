namespace LibraryHolds.Api.Holds;

public enum Format
{
    Hardback,
    Paperback,
    LargePrint,
    Audiobook
}

public enum HoldStatus
{
    Waiting,
    ReadyForCollection,
    Cancelled
}

/// <summary>A member waiting for a copy of a title in a particular format.</summary>
public sealed class Hold
{
    public Guid Id { get; init; }
    public required string MemberId { get; init; }
    public required string Title { get; init; }
    public Format Format { get; init; }
    public DateTimeOffset PlacedAt { get; init; }
    public HoldStatus Status { get; private set; } = HoldStatus.Waiting;
    public Guid? CopyId { get; private set; }
    public DateTimeOffset? CollectBy { get; private set; }

    public bool CanBeCancelled => Status == HoldStatus.Waiting;

    public void Cancel()
    {
        if (!CanBeCancelled)
        {
            throw new InvalidOperationException($"Hold {Id} is {Status}, so it can't be cancelled.");
        }

        Status = HoldStatus.Cancelled;
    }

    public void FillWith(Guid copyId, DateTimeOffset collectBy)
    {
        Status = HoldStatus.ReadyForCollection;
        CopyId = copyId;
        // Postgres stores instants in UTC; responses convert back to UK time.
        CollectBy = collectBy.ToUniversalTime();
    }
}
