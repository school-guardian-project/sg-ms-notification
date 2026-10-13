using System.Text;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ms_notification.Api.Application.Security;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Grpc;
using ms_notification.Api.Infrastructure.Messaging.Kafka;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Persistence.Directory;

namespace ms_notification.Api.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")
            ));

        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Missing Jwt__Secret (set it in .env)");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
        services.AddAuthorization();

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        // El consumidor de student.scanned entrega por IHttpClientFactory (Expo Push).
        services.AddHttpClient();

        services.AddScoped<IEventPublisher, KafkaEventPublisher>();

        // Casos de uso (puertos de entrada).
        services.AddScoped<IProcessScanUseCase, ProcessScanService>();
        services.AddScoped<IGetGuardianNotificationsUseCase, GetGuardianNotificationsService>();
        services.AddScoped<IRegisterDeviceUseCase, RegisterDeviceService>();
        services.AddScoped<IGetStudentCredentialUseCase, GetStudentCredentialService>();

        // Adaptadores de salida: gRPC (ms-route / ms-user-management) y EF.
        services.AddSingleton<IRouteDirectory, RouteGrpcClient>();
        services.AddSingleton<IFamilyDirectory, FamilyGrpcClient>();
        services.AddScoped<IStudentDirectory, StudentDirectory>();

        services.AddSingleton<QrTokenSigner>();

        return services;
    }
}
