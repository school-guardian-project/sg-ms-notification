namespace ms_notification.Api.Domain.Port.Out;

/// <summary>
/// Puerto de salida del canal de push. Lo comparten todos los flujos que avisan
/// al usuario (los escaneos de abordaje/descenso y el incidente de FR-8.2), para
/// que el armado del payload y el manejo de lotes viva en un solo adaptador.
/// </summary>
public interface IPushNotifier
{
    Task SendAsync(
        IReadOnlyCollection<string> deviceTokens,
        string title,
        string? body,
        CancellationToken ct = default);
}
