using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Port.In;

namespace ms_notification.Api.Infrastructure.Messaging.Kafka.Consumer;

/// <summary>
/// Adaptador de entrada de FR-8.2: escucha incident.reported y dispara la entrega
/// por push. Usa su propio groupId para que un atasco de este topic no detenga
/// al consumidor de student.scanned (y viceversa).
/// </summary>
public class IncidentReportedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IncidentReportedConsumer> _logger;
    private IConsumer<string, string>? _consumer;

    public IncidentReportedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<IncidentReportedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "ms-notification-incident-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Application.UseCase.ReportIncidentService.IncidentReportedTopic);

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
                _logger.LogError(ex, "Failed to process an incident.reported message");
            }
        }
    }

    private async Task ProcessMessageAsync(string rawMessage, CancellationToken ct)
    {
        var incident = JsonSerializer.Deserialize<IncidentReportedEvent>(rawMessage);
        if (incident is null) return;

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<INotifyIncidentReportedUseCase>();
        await handler.NotifyAsync(incident, ct);
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}
