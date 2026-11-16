using LibraryHolds.Api.Time;
using LibraryHolds.Scenarios.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace LibraryHolds.Scenarios.Support.App;

/// <summary>
/// The real API, as Program.cs builds it, against a real Postgres in a container. Only the
/// notification provider, which is outside the service, is a stand-in. Start-up is slow, so one app
/// serves every scenario in a feature.
/// </summary>
public sealed class LibraryHoldsApp : IAsyncDisposable
{
    private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BetweenReadings = TimeSpan.FromMilliseconds(50);

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _api;

    public NotificationProviderStandIn NotificationProvider { get; } = new();

    /// <summary>Library time. Each step that does something sets it to when the Gherkin says it happened.</summary>
    public ScenarioClock Clock { get; } = new();

    public HttpClient Api => _api ?? throw new InvalidOperationException("The app has not been started.");

    public async Task StartAsync()
    {
        await _database.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("ConnectionStrings:Library", _database.GetConnectionString());
            host.UseSetting("NotificationProvider:BaseUrl", NotificationProvider.Url);
            // Production polls every 2 seconds; scenarios shouldn't wait that long for each notification.
            host.UseSetting("Outbox:PollInterval", "00:00:00.020");
            host.ConfigureTestServices(services => services.AddSingleton<IClock>(Clock));
        });
        _api = _factory.CreateClient();
    }

    /// <summary>
    /// Waits until the app has finished with everything it has been given: the outbox has nothing left
    /// to send. After this, what the API and the stand-in report is the outcome, not a moment on the way
    /// to it, so "nobody is told anything" needs no guessed delay. Returns are handled inside their
    /// request and write their notification in the same transaction, so an empty outbox is enough here.
    /// A service that also consumed messages would need to wait for its consumers too.
    /// </summary>
    public async Task WaitUntilSettledAsync()
    {
        var deadline = DateTime.UtcNow + SettleTimeout;
        while (true)
        {
            var outbox = await Api.GetFromStrictJsonAsync<OutboxStatusJson>("/admin/outbox");
            if (outbox.Failed > 0)
            {
                // Otherwise "nobody is told anything" would pass because sending broke.
                throw new InvalidOperationException(
                    $"The outbox gave up sending {outbox.Failed} notification(s). Check the test output for the provider's errors.");
            }

            if (outbox.Pending == 0)
            {
                return;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(
                    $"The app had not settled after {SettleTimeout.TotalSeconds}s: the outbox still has {outbox.Pending} notification(s) to send.");
            }

            await Task.Delay(BetweenReadings);
        }
    }

    /// <summary>
    /// Clears what earlier scenarios left behind, so a copy returned in this scenario can't fill a hold
    /// placed in another. Waits for the app to settle first, so nothing sent late lands after the reset.
    /// </summary>
    public async Task ForgetEarlierScenariosAsync()
    {
        await WaitUntilSettledAsync();
        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();
        await using var truncate = new NpgsqlCommand("TRUNCATE holds, copies, outbox_messages", connection);
        await truncate.ExecuteNonQueryAsync();
        NotificationProvider.ForgetEarlierMessages();
    }

    public async ValueTask DisposeAsync()
    {
        _api?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        NotificationProvider.Dispose();
        await _database.DisposeAsync();
    }
}

public sealed class ScenarioClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
