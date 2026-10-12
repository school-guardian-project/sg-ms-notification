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
        services.AddDbContextPool<NotificationContext>(
            (sp, options) => options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)),
            poolSize: 32);

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
        services.AddScoped<GetGuardianNotificationsService>();
        services.AddScoped<RegisterDeviceService>();

        services.AddHttpClient("ms-user-management", client =>
        {
            client.BaseAddress = new Uri(
                configuration["Services:UserManagement:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "Missing Services__UserManagement__BaseUrl (set it in .env)"));
        });

        services.AddHttpClient("ms-route", client =>
        {
            client.BaseAddress = new Uri(
                configuration["Services:Route:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "Missing Services__Route__BaseUrl (set it in .env)"));
        });

        return services;
    }
}
