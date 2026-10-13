using System.Text.Json.Serialization;

namespace ms_notification.Api.Application.Security;

public sealed class QrPayload
{
    [JsonPropertyName("s")] public string StudentProfileId { get; set; } = "";
    [JsonPropertyName("n")] public string StudentName { get; set; } = "";
    [JsonPropertyName("r")] public string RouteId { get; set; } = "";
    [JsonPropertyName("p")] public string RouteStopId { get; set; } = "";
    [JsonPropertyName("i")] public long IssuedAt { get; set; }
    [JsonPropertyName("x")] public long ExpiresAt { get; set; }
}

public sealed record IssuedQrToken(string Token, DateTimeOffset ExpiresAt);