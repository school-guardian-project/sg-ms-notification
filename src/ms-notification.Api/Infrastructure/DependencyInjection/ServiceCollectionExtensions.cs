using AutoMapper;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Configuration;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Application.Security;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Grpc;
using ms_notification.Api.Infrastructure.Security;
using ms_notification.Api.Infrastructure.Messaging.Kafka;
using ms_notification.Api.Infrastructure.Messaging.Observer;
using ms_notification.Api.Infrastructure.Messaging.Push;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Persistence.Directory;

namespace ms_notification.Api.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextPool<NotificationContext>(
            (sp, options) => options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)),
            poolSize: 32);

        // El gateway (Kong) valida el access token RS256 y reenvía la identidad en
        // las cabeceras X-User-*. Aquí solo se confía en ellas para poblar
        // HttpContext.User; sin cabeceras la petición es anónima.
        services.AddGatewayAuthentication();
        services.AddAuthorization();

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        // Ventana horaria del escaneo (ScanWindow__MarginMinutes /
        // ScanWindow__TimeZoneId en .env). Reloj inyectable para probar la regla.
        services.Configure<ScanWindowOptions>(configuration.GetSection(ScanWindowOptions.SectionName));
        services.AddSingleton(TimeProvider.System);

        // El push lo comparten el consumidor de student.scanned y el de
        // incident.reported: el adaptador Expo va detras de IPushNotifier.
        services.AddHttpClient();

        // Observer para eventos de escaneo de estudiantes
        services.AddSingleton<StudentScannedEventObserver>();

        services.AddScoped<IEventPublisher, KafkaEventPublisher>();

        // Casos de uso (puertos de entrada).
        services.AddScoped<IProcessScanUseCase, ProcessScanService>();
        services.AddScoped<IGetGuardianNotificationsUseCase, GetGuardianNotificationsService>();
        services.AddScoped<IRegisterDeviceUseCase, RegisterDeviceService>();
        services.AddScoped<IGetStudentCredentialUseCase, GetStudentCredentialService>();
        services.AddScoped<IReportIncidentUseCase, ReportIncidentService>();
        services.AddScoped<INotifyIncidentReportedUseCase, NotifyIncidentReportedService>();

        // Adaptadores de salida: gRPC (ms-route / ms-user-management), EF y Expo.
        services.AddSingleton<IRouteDirectory, RouteGrpcClient>();
        services.AddSingleton<IFamilyDirectory, FamilyGrpcClient>();
        services.AddSingleton<IPushNotifier, ExpoPushNotifier>();
        services.AddScoped<IStudentDirectory, StudentDirectory>();
        services.AddScoped<ISchoolDirectory, SchoolDirectory>();

        services.AddSingleton<QrTokenSigner>();

        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<IncidentProfile>();
        });

        // Casos de uso adicionales para dashboard admin
        services.AddScoped<IGetAlertsUseCase, GetAlertsService>();
        services.AddScoped<IGetIncidentsUseCase, GetIncidentsService>();
        services.AddScoped<IAcknowledgeAlertUseCase, AcknowledgeAlertService>();
        services.AddScoped<IResolveIncidentUseCase, ResolveIncidentService>();
        services.AddScoped<IGetAlertByIdUseCase, GetAlertByIdService>();
        services.AddScoped<IGetIncidentByIdUseCase, GetIncidentByIdService>();

        return services;
    }
}
