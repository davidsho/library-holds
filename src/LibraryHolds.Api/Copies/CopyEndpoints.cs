using LibraryHolds.Api.Data;
using LibraryHolds.Api.Holds;

namespace LibraryHolds.Api.Copies;

public sealed record ReturnCopyRequest(Guid? CopyId, string? Title, Format? Format)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (CopyId is null || CopyId == Guid.Empty)
        {
            errors[nameof(CopyId)] = ["A returned copy needs its barcode."];
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            errors[nameof(Title)] = ["A returned copy needs a title."];
        }

        if (Format is null)
        {
            errors[nameof(Format)] = ["A returned copy needs a format."];
        }

        return errors;
    }
}

public sealed record CopyResponse(Guid Id, string Title, Format Format, CopyLocation Location, Guid? HoldId)
{
    public static CopyResponse From(Copy copy) => new(copy.Id, copy.Title, copy.Format, copy.Location, copy.HoldId);
}

public static class CopyEndpoints
{
    public static void MapCopyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/returns", ReturnAsync);
        app.MapGet("/copies/{id:guid}", GetAsync);
    }

    private static async Task<IResult> ReturnAsync(ReturnCopyRequest request, Returns returns, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var copy = await returns.ReceiveAsync(request.CopyId!.Value, request.Title!, request.Format!.Value, cancellationToken);
        return Results.Ok(CopyResponse.From(copy));
    }

    private static async Task<IResult> GetAsync(Guid id, LibraryDbContext db, CancellationToken cancellationToken) =>
        await db.Copies.FindAsync([id], cancellationToken) is { } copy
            ? Results.Ok(CopyResponse.From(copy))
            : Results.NotFound();
}
