using Microsoft.AspNetCore.Authentication;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Opciones del esquema de autenticacion basado en las cabeceras del gateway.
/// </summary>
public sealed class GatewayAuthenticationOptions : AuthenticationSchemeOptions
{
}
