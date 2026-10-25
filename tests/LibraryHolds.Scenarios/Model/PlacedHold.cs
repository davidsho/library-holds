namespace LibraryHolds.Scenarios.Model;

/// <summary>A hold a scenario placed: who for, what, and when. The id is the one the API gave it.</summary>
public sealed record PlacedHold(Guid Id, string Member, string Title, string Format, DateTimeOffset PlacedAt);
