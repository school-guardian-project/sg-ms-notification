using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Tests.Fakes;

/// <summary>Un push capturado: a que dispositivos iba, con que titulo y cuerpo.</summary>
internal sealed record SentPush(IReadOnlyList<string> DeviceTokens, string Title, string? Body);

/// <summary>Captura los push que se habrian enviado a Expo, sin tocar la red.</summary>
internal sealed class RecordingPushNotifier : IPushNotifier
{
    public List<SentPush> Sent { get; } = new();

    public Task SendAsync(
        IReadOnlyCollection<string> deviceTokens,
        string title,
        string? body,
        CancellationToken ct = default)
    {
        Sent.Add(new SentPush(deviceTokens.ToList(), title, body));
        return Task.CompletedTask;
    }
}
