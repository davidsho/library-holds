using System.Text.Json;
using LibraryHolds.Api.Data;
using LibraryHolds.Api.Holds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryHolds.Api.Notifications;

/// <summary>What the notification provider is asked to tell a member whose hold is ready.</summary>
public sealed record HoldReadyNotification(string MemberId, Guid HoldId, string Title, Format Format, DateTimeOffset CollectBy);

/// <summary>A notification waiting to be sent, written in the same transaction as the change that caused it.</summary>
public sealed class OutboxMessage
{
    public long Id { get; private set; }
    public required string Payload { get; init; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }

    public static OutboxMessage For(HoldReadyNotification notification) =>
        new() { Payload = JsonSerializer.Serialize(notification, NotificationProvider.Json) };

    public void MarkSent(DateTimeOffset at) => SentAt = at;

    public void RecordFailure(string error, DateTimeOffset at, int maxAttempts)
    {
        Attempts++;
        LastError = error;
        if (Attempts >= maxAttempts)
        {
            FailedAt = at;
        }
    }
}

public sealed class OutboxOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxAttempts { get; set; } = 5;
}

/// <summary>Sends outbox messages to the notification provider, oldest first, until each is sent or given up on.</summary>
public sealed class OutboxDispatcher(IServiceScopeFactory scopes, IOptions<OutboxOptions> options, ILogger<OutboxDispatcher> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SendPendingAsync(stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Sending outbox messages failed");
            }
        }
    }

    // SentAt and FailedAt record when the outbox did its work, not library time, so they don't use IClock.
    private async Task SendPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var provider = scope.ServiceProvider.GetRequiredService<NotificationProvider>();

        var pending = await db.Outbox
            .Where(m => m.SentAt == null && m.FailedAt == null)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await provider.SendHoldReadyAsync(message.Payload, cancellationToken);
                message.MarkSent(DateTimeOffset.UtcNow);
            }
            catch (HttpRequestException e)
            {
                logger.LogWarning(e, "Outbox message {Id} could not be sent", message.Id);
                message.RecordFailure(e.Message, DateTimeOffset.UtcNow, options.Value.MaxAttempts);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed record OutboxStatusResponse(int Pending, int Sent, int Failed);

public static class OutboxEndpoints
{
    /// <summary>How far the outbox has got, for operators and for tests that need to know it has finished.</summary>
    public static void MapOutboxEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/admin/outbox", async (LibraryDbContext db, CancellationToken cancellationToken) =>
            new OutboxStatusResponse(
                await db.Outbox.CountAsync(m => m.SentAt == null && m.FailedAt == null, cancellationToken),
                await db.Outbox.CountAsync(m => m.SentAt != null, cancellationToken),
                await db.Outbox.CountAsync(m => m.FailedAt != null, cancellationToken)));
}
