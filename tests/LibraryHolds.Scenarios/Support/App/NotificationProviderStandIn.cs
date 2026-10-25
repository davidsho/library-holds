using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LibraryHolds.Scenarios.Support.App;

/// <summary>Stands in for the notification provider: accepts what the API sends, and remembers it.</summary>
public sealed class NotificationProviderStandIn : IDisposable
{
    private const string HoldReadyPath = "/messages/hold-ready";

    private readonly WireMockServer _server = WireMockServer.Start();

    public NotificationProviderStandIn() => AcceptHoldReadyMessages();

    public string Url => _server.Url!;

    /// <summary>The raw JSON of every hold-ready message received since the last reset, oldest first.</summary>
    public IReadOnlyList<string> HoldReadyMessages() =>
        _server.LogEntries
            .Select(e => e.RequestMessage)
            .OfType<WireMock.IRequestMessage>()
            .Where(r => r.Path == HoldReadyPath)
            .OrderBy(r => r.DateTime)
            .Select(r => r.Body ?? "")
            .ToList();

    public void ForgetEarlierMessages() => _server.ResetLogEntries();

    public void Dispose() => _server.Stop();

    private void AcceptHoldReadyMessages() =>
        _server
            .Given(Request.Create().WithPath(HoldReadyPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(202));
}
