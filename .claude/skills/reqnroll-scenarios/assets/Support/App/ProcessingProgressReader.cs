// Drop-in from the reqnroll-scenarios skill.
// Replace the YourService namespace and AppUnderTest with the target repo's names.
using System.Globalization;

using Confluent.Kafka;
using Confluent.Kafka.Admin;

using Dapper;

using Microsoft.Data.SqlClient;

namespace YourService.Tests.Integration.Scenarios.Support.App;

/// <summary>Reads how far the service has got: each consumer's place in its topic, and the outbox's backlog.</summary>
internal sealed class ProcessingProgressReader(
    IAdminClient kafkaAdmin,
    IReadOnlyList<KafkaConsumer> consumers,
    string sqlConnectionString)
{
    public async Task<ProcessingProgress> ReadAsync(CancellationToken cancellationToken)
    {
        var consumerProgress = await Task.WhenAll(consumers.Select(ReadConsumerProgressAsync));
        var outbox = await ReadOutboxProgressAsync(cancellationToken);

        // A dead-lettered event will never be published, so a scenario expecting none would pass for the wrong reason.
        if (outbox.DeadLettered > 0)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The outbox gave up publishing {outbox.DeadLettered} event(s); see LastError in dbo.OutboxMessage."));
        }

        return new ProcessingProgress(consumerProgress, outbox);
    }

    private async Task<ConsumerProgress> ReadConsumerProgressAsync(KafkaConsumer consumer)
    {
        // Every topic has a single partition (see AppUnderTest.CreateTopicsAsync).
        var partition = new TopicPartition(consumer.Topic, new Partition(0));

        var endOfTopic = await kafkaAdmin.ListOffsetsAsync(
        [
            new TopicPartitionOffsetSpec
            {
                TopicPartition = partition,
                OffsetSpec = OffsetSpec.Latest()
            }
        ]);

        var committed = await kafkaAdmin.ListConsumerGroupOffsetsAsync(
            [new ConsumerGroupTopicPartitions(consumer.GroupId, [partition])]);

        var handled = committed.Single().Partitions.Single().Offset;

        return new ConsumerProgress(
            consumer.Name,
            Published: endOfTopic.ResultInfos.Single().TopicPartitionOffsetError.Offset.Value,
            // A consumer that has committed nothing yet has handled nothing.
            Handled: handled.IsSpecial ? 0 : handled.Value);
    }

    private async Task<OutboxProgress> ReadOutboxProgressAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(sqlConnectionString);

        return await connection.QuerySingleAsync<OutboxProgress>(new CommandDefinition(
            """
            SELECT ISNULL(MAX(RowId), 0) AS LastRowId,
                   COUNT(CASE WHEN ProcessedOnUtc IS NULL AND DeadLetteredOnUtc IS NULL THEN 1 END) AS Unpublished,
                   COUNT(DeadLetteredOnUtc) AS DeadLettered
            FROM dbo.OutboxMessage
            """,
            cancellationToken: cancellationToken));
    }
}
