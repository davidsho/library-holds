using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibraryHolds.Api.Notifications;

/// <summary>The external service that sends members their emails and texts.</summary>
public sealed class NotificationProvider(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SendHoldReadyAsync(string payload, CancellationToken cancellationToken)
    {
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await http.PostAsync("messages/hold-ready", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
