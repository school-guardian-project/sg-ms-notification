using System.Security.Claims;

namespace ms_notification.Api.Infrastructure.Controller;

/// <summary>
/// Lectura del claim "sub" que el gateway (Kong) inyecta tras validar el token:
/// el id del perfil autenticado, que los controladores pasan como entrada a su
/// caso de uso. La autorizacion (rol y perfil) vive en los atributos de
/// Infrastructure/Security (RequireRole / RequireProfile).
/// </summary>
internal static class NotificationClaims
{
    internal static Guid CurrentProfileId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
