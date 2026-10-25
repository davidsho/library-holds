namespace LibraryHolds.Scenarios.Model;

// The API's JSON, field by field, written out independently of the API's own response types. Read
// with StrictJson, so adding, dropping or renaming a field fails a scenario until these are updated.
// Enums and instants stay as the strings the API sends, so their spelling and offsets are part of
// the contract too.

/// <summary>A hold, as GET /holds/{id} and POST /holds return it.</summary>
public sealed record HoldJson
{
    public required Guid Id { get; init; }
    public required string MemberId { get; init; }
    public required string Title { get; init; }
    public required string Format { get; init; }
    public required string Status { get; init; }
    public required string PlacedAt { get; init; }
    public required Guid? CopyId { get; init; }
    public required string? CollectBy { get; init; }
}

/// <summary>A copy, as GET /copies/{id} and POST /returns return it.</summary>
public sealed record CopyJson
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Format { get; init; }
    public required string Location { get; init; }
    public required Guid? HoldId { get; init; }
}

/// <summary>What the API posts to the notification provider when a hold is ready.</summary>
public sealed record HoldReadyMessageJson
{
    public required string MemberId { get; init; }
    public required Guid HoldId { get; init; }
    public required string Title { get; init; }
    public required string Format { get; init; }
    public required string CollectBy { get; init; }
}

/// <summary>GET /admin/outbox: how far the outbox has got.</summary>
public sealed record OutboxStatusJson
{
    public required int Pending { get; init; }
    public required int Sent { get; init; }
    public required int Failed { get; init; }
}
