using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Infrastructure.Messaging.Kafka.Consumer;

public class StudentScannedConsumer : BackgroundService
{
    private const string PushEndpoint = "https://exp.host/--/api/v2/push/send";

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StudentScannedConsumer> _logger;
    private IConsumer<string, string>? _consumer;

    public StudentScannedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<StudentScannedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
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

                var data = JsonSerializer.Deserialize<StudentScannedEvent>(result.Message.Value);
                if (data == null) continue;

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();

                var alert = context.Alerts.FirstOrDefault(a => a.Id == data.AlertId);
                if (alert == null) continue;

                var familyMembers = await GetFamilyMembersAsync(data.StudentProfileId);
                foreach (var memberProfileId in familyMembers)
                {
                    context.AlertRecipients.Add(new Domain.Model.AlertRecipient
                    {
                        Id = Guid.NewGuid(),
                        AlertId = data.AlertId,
                        ProfileId = memberProfileId,
                        DateTimeRead = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();

                try
                {
                    var tokens = await GetDeviceTokensAsync(context, familyMembers);
                    if (tokens.Count > 0)
                    {
                        await SendPushAsync(tokens, alert.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not push the notification for alert {AlertId}", data.AlertId);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
        }
    }

    private async Task<List<string>> GetDeviceTokensAsync(NotificationContext context, List<Guid> profileIds)
    {
        if (profileIds.Count == 0)
        {
            return new List<string>();
        }

        return await context.DeviceTokens
            .AsNoTracking()
            .Where(t => profileIds.Contains(t.ProfileId))
            .Select(t => t.ExpoToken)
            .Distinct()
            .ToListAsync();
    }

    private async Task SendPushAsync(IReadOnlyCollection<string> tokens, string? body)
    {
        var client = _httpClientFactory.CreateClient();

        foreach (var token in tokens)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    to = token,
                    title = "Notificación de ruta",
                    body = body ?? string.Empty,
                    sound = "default"
                });

                var response = await client.PostAsync(PushEndpoint, new StringContent(payload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Expo push returned {StatusCode} for token {Token}", response.StatusCode, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not deliver the push notification to {Token}", token);
            }
        }
    }

    private async Task<List<Guid>> GetFamilyMembersAsync(Guid studentProfileId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("ms-user-management");

            var response = await client.GetAsync($"/api/families/student/{studentProfileId}/members");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch family members for student {StudentProfileId}: {StatusCode}",
                    studentProfileId, response.StatusCode);
                return new List<Guid>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var members = JsonSerializer.Deserialize<List<FamilyMemberResponse>>(content);

            return members?.Select(m => m.ProfileId).ToList() ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching family members for student {StudentProfileId}", studentProfileId);
            return new List<Guid>();
        }
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }

    private class FamilyMemberResponse
    {
        public Guid ProfileId { get; set; }
    }
}
