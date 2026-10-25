namespace LibraryHolds.Scenarios.Model;

/// <summary>A copy a scenario returned. It carries every detail the matching rule depends on, so a step can vary one with <c>with</c>.</summary>
public sealed record ReturnedCopy(Guid Id, string Title, string Format, DateTimeOffset ReturnedAt);
