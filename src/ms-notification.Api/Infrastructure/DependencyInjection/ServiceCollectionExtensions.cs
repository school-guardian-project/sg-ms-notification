using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Infrastructure.Messaging.Kafka;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")
            ));

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        services.AddScoped<IEventPublisher, KafkaEventPublisher>();
        services.AddScoped<ProcessScanService>();
        services.AddHttpClient();

        return services;
    }
}
