namespace LibraryHolds.Scenarios.Model;

/// <summary>A copy a scenario returned.</summary>
public sealed record ReturnedCopy(Guid Id, string Title, string Format, DateTimeOffset ReturnedAt);
