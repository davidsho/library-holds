// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
namespace YourService.Tests.Integration.Scenarios.Support.App;

/// <summary>
/// How far one of the service's Kafka consumers has got through its topic. A consumer commits a
/// message only once it has finished handling it, so <see cref="Handled"/> never counts work in progress.
/// </summary>
public sealed record ConsumerProgress(string Consumer, long Published, long Handled)
{
    public bool HasHandledEverything => Handled >= Published;

    public override string ToString() => $"{Consumer} has handled {Handled} of {Published} messages";
}
