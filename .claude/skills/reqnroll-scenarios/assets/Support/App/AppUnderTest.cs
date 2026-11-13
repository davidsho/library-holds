// Template from the reqnroll-scenarios skill.
// Adapt before use; it will not compile as it stands:
//   - rename AppUnderTest to <Service>App (e.g. ShopApp) and fix the namespace;
//   - the WebApplicationFactory<T> marker type (often Program or IApiMarker);
//   - containers: drop Kafka or SQL Server if unused, add others (e.g. Testcontainers.Redis);
//   - topics, EveryConsumer and the Kafka:Consumers:* setting keys, from the service's appsettings;
//   - one stand-in class per external system (see NotificationProviderStandIn in library-holds);
//   - the database: a <Service>Database class that creates the schema and holds the scenario queries;
//   - the outbox settings, if the service uses an outbox (the settle check reads dbo.OutboxMessage).
// If the repo already has a WebApplicationFactory that does most of this, extend it instead.
using System.Globalization;
using System.Text;

using Confluent.Kafka;
using Confluent.Kafka.Admin;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Testcontainers.Kafka;
using Testcontainers.MsSql;

namespace YourService.Tests.Integration.Scenarios.Support.App;

/// <summary>
/// The whole service, as Program.cs builds it, running against real Kafka and SQL Server containers, with
/// WireMock standing in for the external systems it calls.
/// <para>
/// Every feature gets the same database, topics and stand-ins, so every driver works against every feature's
/// app. A feature's app only says which consumers run (see <see cref="ConsumersSwitchedOn"/>): the consumers
/// are what make one journey through the system differ from another.
/// </para>
/// </summary>
public abstract class AppUnderTest : WebApplicationFactory<Program>
{
    // These match appsettings.test.json, which the app loads on top of its own appsettings.json.
    protected const string SomeInputTopic = "test.<domain>.<topic>";

    private const string OutboxTopicPrefix = "test";
    private const string SomeWebhookApiKey = "test-<webhook>-api-key";

    private static readonly Uri SomeWebhookPath = new("/api/v1/webhooks/<provider>/<event>", UriKind.Relative);

    private static readonly TimeSpan HowLongToWaitForTheAppToSettle = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan HowOftenToCheckWhetherTheAppHasSettled = TimeSpan.FromMilliseconds(250);

    private static readonly string[] Topics = [SomeInputTopic];

    /// <summary>Every consumer these scenarios know about. Any a feature does not switch on is switched off.</summary>
    private static readonly string[] EveryConsumer = ["<ConsumerName>"];

    // apache/kafka-native is KRaft-only and publishes linux/arm64 + linux/amd64.
    private readonly KafkaContainer _kafka = new KafkaBuilder("apache/kafka-native:3.9.1").WithKRaft().Build();
    private readonly MsSqlContainer _sql = new MsSqlBuilder().Build();

    private IAdminClient? _kafkaAdmin;

    /// <summary>What the external system has been sent. One stand-in per system, named after it.</summary>
    public SomeExternalSystemStandIn SomeExternalSystem { get; } = new();

    private string SqlConnectionString => _sql.GetConnectionString();

    private string KafkaBootstrapAddress => _kafka.GetBootstrapAddress();

    /// <summary>The consumers this feature runs, by name, each with the topic it reads.</summary>
    protected abstract IReadOnlyDictionary<string, string> ConsumersSwitchedOn { get; }

    /// <summary>
    /// Starts a new app. If start-up fails the app is disposed before the exception is rethrown, so its
    /// containers do not outlive a feature that never got to run.
    /// </summary>
    public static async Task<TApp> StartNewAsync<TApp>() where TApp : AppUnderTest, new()
    {
        var app = new TApp();

        try
        {
            await app.StartAsync();
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            _kafkaAdmin?.Dispose();
            SomeExternalSystem.Dispose();
            await Task.WhenAll(_kafka.DisposeAsync().AsTask(), _sql.DisposeAsync().AsTask());
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Waits until the service has finished with everything it has been given: every consumer this feature
    /// runs has handled every message on its topic, and the outbox has published every event queued for it.
    /// Anything read after this is the outcome, not a moment on the way to it, so a step can read once
    /// instead of polling, and can show something did <em>not</em> happen without a fixed sleep.
    /// </summary>
    public async Task WaitUntilSettledAsync(CancellationToken cancellationToken = default)
    {
        var progress = new ProcessingProgressReader(KafkaAdmin(), SwitchedOnConsumers(), SqlConnectionString);
        var deadline = DateTime.UtcNow + HowLongToWaitForTheAppToSettle;
        var current = await progress.ReadAsync(cancellationToken);
        ProcessingProgress earlier;

        do
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The service had not settled after {HowLongToWaitForTheAppToSettle.TotalSeconds}s: {current}"));
            }

            earlier = current;
            await Task.Delay(HowOftenToCheckWhetherTheAppHasSettled, cancellationToken);
            current = await progress.ReadAsync(cancellationToken);
        } while (!current.HasSettledSince(earlier));
    }

    // ── Kafka ───────────────────────────────────────────────────────────────────────────────────
    // Publish through the app's own publishers where it has them, and read output topics straight off Kafka.

    // ── Webhooks ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Posts to the endpoint the provider calls, authenticated as the provider.</summary>
    public Task<HttpResponseMessage> PostSomeWebhookAsync(string json) =>
        PostWebhookAsync(SomeWebhookPath, SomeWebhookApiKey, json, new Dictionary<string, string>(StringComparer.Ordinal));

    // ── Setup ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Feature flags and other services a feature needs replacing. Nothing by default.</summary>
    protected virtual void ReplaceServices(IServiceCollection services)
    {
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddJsonFile("appsettings.test.json");
            config.AddInMemoryCollection(Settings());
        });

        // Remove hosted services no scenario uses here, before the feature's own replacements.
        builder.ConfigureServices(ReplaceServices);

        return base.CreateHost(builder);
    }

    private async Task StartAsync()
    {
        await Task.WhenAll(_kafka.StartAsync(), _sql.StartAsync());
        // await <Service>Database.CreateAsync(SqlConnectionString);
        await CreateTopicsAsync();
        // SomeExternalSystem.Accept<Requests>();

        // Resolving Services builds and starts the host, and with it the consumers.
        _ = Services;
    }

    // Every topic has a single partition, which ProcessingProgressReader relies on. Producers built with
    // AllowAutoCreateTopics=false, such as the outbox's, would otherwise fail.
    private Task CreateTopicsAsync() =>
        KafkaAdmin().CreateTopicsAsync(
            Topics.Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = 1,
                ReplicationFactor = 1
            }));

    private Dictionary<string, string?> Settings()
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ConnectionStrings:<Name>"] = SqlConnectionString,
            ["Kafka:BootstrapServers"] = KafkaBootstrapAddress,
            ["Outbox:TopicPrefix"] = OutboxTopicPrefix,
            // Production polls every 5s; 1s keeps the scenarios quick.
            ["Outbox:PollInterval"] = "00:00:01",
            ["<ExternalSystem>:BaseUrl"] = SomeExternalSystem.Url.ToString()
        };

        foreach (var consumer in SwitchedOnConsumers())
        {
            var section = $"Kafka:Consumers:{consumer.Name}";
            settings[$"{section}:Enabled"] = "true";
            settings[$"{section}:Topic"] = consumer.Topic;
            settings[$"{section}:GroupId"] = consumer.GroupId;
            settings[$"{section}:AutoOffsetReset"] = "Earliest";
        }

        foreach (var consumer in EveryConsumer.Except(ConsumersSwitchedOn.Keys, StringComparer.Ordinal))
        {
            settings[$"Kafka:Consumers:{consumer}:Enabled"] = "false";
        }

        return settings;
    }

    /// <summary>The settle check reads each consumer's committed offset by this group id.</summary>
    private KafkaConsumer[] SwitchedOnConsumers() =>
    [
        .. ConsumersSwitchedOn.Select(consumer =>
            new KafkaConsumer(consumer.Key, consumer.Value, GroupId: $"{consumer.Key}-reqnroll"))
    ];

    private async Task<HttpResponseMessage> PostWebhookAsync(
        Uri path,
        string apiKey,
        string json,
        IReadOnlyDictionary<string, string> extraHeaders)
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        foreach (var (name, value) in extraHeaders)
        {
            client.DefaultRequestHeaders.Add(name, value);
        }

        return await client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private IAdminClient KafkaAdmin() =>
        _kafkaAdmin ??= new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = KafkaBootstrapAddress
        }).Build();
}
