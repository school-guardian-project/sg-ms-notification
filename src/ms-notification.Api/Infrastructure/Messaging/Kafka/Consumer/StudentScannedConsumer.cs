using System.Text.Json;
using Confluent.Kafka;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Infrastructure.Messaging.Kafka.Consumer;

public class StudentScannedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConsumer<string, string>? _consumer;

    public StudentScannedConsumer(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
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
            }
            catch (ConsumeException ex)
            {
                Console.Error.WriteLine($"Kafka consume error: {ex.Error.Reason}");
            }
        }
    }

    private async Task<List<Guid>> GetFamilyMembersAsync(Guid studentProfileId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient();

            var response = await client.GetAsync($"http://ms-user-management:8080/api/v1/families/student/{studentProfileId}");
            if (!response.IsSuccessStatusCode) return new List<Guid>();

            var content = await response.Content.ReadAsStringAsync();
            var familyMembers = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(content);
            return familyMembers ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}
