using LibraryHolds.Api.Holds;

namespace LibraryHolds.Api.Copies;

public enum CopyLocation
{
    Shelf,
    HoldShelf
}

/// <summary>One physical copy of a title, identified by its barcode.</summary>
public sealed class Copy
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public Format Format { get; init; }
    public CopyLocation Location { get; private set; }
    public Guid? HoldId { get; private set; }
    public DateTimeOffset LastReturnedAt { get; private set; }

    public void PutOnShelf(DateTimeOffset returnedAt)
    {
        Location = CopyLocation.Shelf;
        HoldId = null;
        LastReturnedAt = returnedAt;
    }

    public void PutOnHoldShelfFor(Hold hold, DateTimeOffset returnedAt)
    {
        Location = CopyLocation.HoldShelf;
        HoldId = hold.Id;
        LastReturnedAt = returnedAt;
    }
}
