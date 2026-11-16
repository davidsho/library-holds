// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
namespace YourService.Tests.Integration.Scenarios.Support.App;

/// <summary>One of the service's Kafka consumers, as a feature's app switches it on.</summary>
public sealed record KafkaConsumer(string Name, string Topic, string GroupId);
