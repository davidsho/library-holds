// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
namespace YourService.Tests.Integration.Scenarios.Support.App;

/// <summary>
/// How far the outbox dispatcher has got. A row counts as published only once Kafka has acknowledged it,
/// so <see cref="Unpublished"/> includes any event the dispatcher is part-way through sending.
/// </summary>
public sealed record OutboxProgress(long LastRowId, int Unpublished, int DeadLettered)
{
    public override string ToString() => $"the outbox has {Unpublished} event(s) still to publish";
}
