using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Api.Infrastructure.Messaging.Push;

/// <summary>
/// Adaptador del canal de push sobre el endpoint de Expo. Arma un mensaje por
/// token y los manda en lotes de 100, que es el maximo que acepta Expo. Un lote
/// que falla se registra y no tumba al resto: el fan-out es best-effort y la
/// alerta ya quedo persistida antes de empujar.
/// </summary>
public class ExpoPushNotifier : IPushNotifier
{
    private const int ExpoBatchSize = 100;

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExpoPushNotifier> _logger;

    public ExpoPushNotifier(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<ExpoPushNotifier> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(
        IReadOnlyCollection<string> deviceTokens,
        string title,
        string? body,
        CancellationToken ct = default)
    {
        if (deviceTokens.Count == 0)
        {
            return;
        }

        var endpoint = _configuration["Expo:PushEndpoint"]
            ?? throw new InvalidOperationException("Missing Expo__PushEndpoint (set it in .env)");

        var client = _httpClientFactory.CreateClient();

        foreach (var chunk in deviceTokens.Chunk(ExpoBatchSize))
        {
            var messages = chunk.Select(token => new
            {
                to = token,
                title,
                body = body ?? string.Empty,
                sound = "default"
            });

            try
            {
                var payload = JsonSerializer.Serialize(messages);
                var response = await client.PostAsync(
                    endpoint, new StringContent(payload, Encoding.UTF8, "application/json"), ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Expo push returned {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not deliver a push batch");
            }
        }
    }
}
