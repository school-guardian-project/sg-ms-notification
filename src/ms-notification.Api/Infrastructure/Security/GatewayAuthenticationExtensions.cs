namespace ms_notification.Api.Infrastructure.Security;

public static class GatewayAuthenticationExtensions
{
    /// <summary>
    /// Registra el esquema "Gateway" como esquema de autenticacion por defecto.
    /// </summary>
    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(GatewayAuthenticationHandler.SchemeName)
            .AddScheme<GatewayAuthenticationOptions, GatewayAuthenticationHandler>(
                GatewayAuthenticationHandler.SchemeName, _ => { });

        return services;
    }
}
