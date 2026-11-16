using LibraryHolds.Api.Data;
using LibraryHolds.Api.Time;

namespace LibraryHolds.Api.Holds;

public sealed record PlaceHoldRequest(string? MemberId, string? Title, Format? Format)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(MemberId))
        {
            errors[nameof(MemberId)] = ["A hold needs the member who placed it."];
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            errors[nameof(Title)] = ["A hold needs a title."];
        }

        if (Format is null)
        {
            errors[nameof(Format)] = ["A hold needs a format."];
        }

        return errors;
    }
}

public sealed record HoldResponse(
    Guid Id,
    string MemberId,
    string Title,
    Format Format,
    HoldStatus Status,
    DateTimeOffset PlacedAt,
    Guid? CopyId,
    DateTimeOffset? CollectBy)
{
    public static HoldResponse From(Hold hold) => new(
        hold.Id,
        hold.MemberId,
        hold.Title,
        hold.Format,
        hold.Status,
        hold.PlacedAt,
        hold.CopyId,
        hold.CollectBy is { } collectBy ? UkTime.From(collectBy) : null);
}

public static class HoldEndpoints
{
    public static void MapHoldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/holds", PlaceAsync);
        app.MapGet("/holds/{id:guid}", GetAsync);
        app.MapDelete("/holds/{id:guid}", CancelAsync);
    }

    private static async Task<IResult> PlaceAsync(PlaceHoldRequest request, LibraryDbContext db, IClock clock, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var hold = new Hold
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId!,
            Title = request.Title!,
            Format = request.Format!.Value,
            PlacedAt = clock.UtcNow
        };
        db.Holds.Add(hold);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/holds/{hold.Id}", HoldResponse.From(hold));
    }

    private static async Task<IResult> GetAsync(Guid id, LibraryDbContext db, CancellationToken cancellationToken) =>
        await db.Holds.FindAsync([id], cancellationToken) is { } hold
            ? Results.Ok(HoldResponse.From(hold))
            : Results.NotFound();

    private static async Task<IResult> CancelAsync(Guid id, LibraryDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Holds.FindAsync([id], cancellationToken) is not { } hold)
        {
            return Results.NotFound();
        }

        if (!hold.CanBeCancelled)
        {
            return Results.Conflict();
        }

        hold.Cancel();
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
