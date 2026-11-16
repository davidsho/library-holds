// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
namespace YourService.Tests.Integration.Scenarios.Support.App;

/// <summary>
/// How far the service has got with the work it has been given, read at one moment: each consumer's
/// place in its topic, and what the outbox still has to publish.
/// </summary>
public sealed record ProcessingProgress(IReadOnlyList<ConsumerProgress> Consumers, OutboxProgress Outbox)
{
    /// <summary>
    /// Idle now, and nothing moved since the earlier reading. Every count here only ever grows, so two equal
    /// readings mean no message was published, handled or queued in between; one idle reading on its own
    /// could have been taken in the gap between a consumer queuing an event and the outbox publishing it.
    /// </summary>
    public bool HasSettledSince(ProcessingProgress earlier) =>
        IsIdle && Outbox == earlier.Outbox && Consumers.SequenceEqual(earlier.Consumers);

    public override string ToString() => string.Join("; ", Consumers.Append<object>(Outbox));

    private bool IsIdle => Outbox.Unpublished == 0 && Consumers.All(consumer => consumer.HasHandledEverything);
}
