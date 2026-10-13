using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Infrastructure.Messaging.Kafka.Consumer;

public class StudentScannedConsumer : BackgroundService
{
    private const int ExpoBatchSize = 100;

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IFamilyDirectory _familyDirectory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StudentScannedConsumer> _logger;
    private IConsumer<string, string>? _consumer;

    public StudentScannedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IFamilyDirectory familyDirectory,
        IHttpClientFactory httpClientFactory,
        ILogger<StudentScannedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _familyDirectory = familyDirectory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "ms-notification-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe("student.scanned");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message == null) continue;

                await ProcessMessageAsync(result.Message.Value, stoppingToken);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process a student.scanned message");
            }
        }
    }

    private async Task ProcessMessageAsync(string rawMessage, CancellationToken ct)
    {
        var data = JsonSerializer.Deserialize<StudentScannedEvent>(rawMessage);
        if (data is null) return;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();

        var alert = await context.Alerts.FirstOrDefaultAsync(a => a.Id == data.AlertId, ct);
        if (alert is null) return;

        var parents = await GetFamilyMembersAsync(data.StudentProfileId, ct);
        
        var already = await context.AlertRecipients
            .Where(r => r.AlertId == data.AlertId)
            .Select(r => r.ProfileId)
            .ToListAsync(ct);

        var newParents = parents.Where(p => !already.Contains(p)).ToList();
        foreach (var memberProfileId in newParents)
        {
            context.AlertRecipients.Add(new AlertRecipient
            {
                Id = Guid.NewGuid(),
                AlertId = data.AlertId,
                ProfileId = memberProfileId,
                DateTimeRead = DateTime.UtcNow
            });
        }

        if (newParents.Count > 0)
        {
            await context.SaveChangesAsync(ct);
        }

        try
        {
            var tokens = await GetDeviceTokensAsync(context, parents, ct);
            if (tokens.Count > 0)
            {
                await SendPushAsync(tokens, alert.Description, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not push the notification for alert {AlertId}", data.AlertId);
        }
    }

    private async Task<List<Guid>> GetFamilyMembersAsync(Guid studentProfileId, CancellationToken ct)
    {
        try
        {
            var ids = await _familyDirectory.GetParentProfileIdsAsync(studentProfileId, ct);
            return ids.ToList();
        }
        catch (Exception ex)
        {
            // Es un consumidor en segundo plano: se registra y no se pierde la alerta.
            _logger.LogError(ex, "Error fetching family members for student {StudentProfileId}", studentProfileId);
            return new List<Guid>();
        }
    }

    private static Task<List<string>> GetDeviceTokensAsync(
        NotificationContext context, List<Guid> profileIds, CancellationToken ct)
    {
        if (profileIds.Count == 0)
        {
            return Task.FromResult(new List<string>());
        }

        return context.DeviceTokens
            .AsNoTracking()
            .Where(t => profileIds.Contains(t.ProfileId))
            .Select(t => t.ExpoToken)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task SendPushAsync(IReadOnlyCollection<string> tokens, string? body, CancellationToken ct)
    {
        var endpoint = _configuration["Expo:PushEndpoint"]
            ?? throw new InvalidOperationException("Missing Expo__PushEndpoint (set it in .env)");

        var client = _httpClientFactory.CreateClient();

        foreach (var chunk in tokens.Chunk(ExpoBatchSize))
        {
            var messages = chunk.Select(token => new
            {
                to = token,
                title = "Notificación de ruta",
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

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}
