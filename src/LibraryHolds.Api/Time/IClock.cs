namespace LibraryHolds.Api.Time;

/// <summary>The time things happen in the library: when a hold is placed, when a copy is returned.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
