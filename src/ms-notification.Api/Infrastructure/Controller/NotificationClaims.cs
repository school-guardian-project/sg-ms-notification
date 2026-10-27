using System.Security.Claims;
using ms_notification.Api.Infrastructure.Auth;

namespace ms_notification.Api.Infrastructure.Controller;

/// <summary>
/// Lectura del claim "sub" del access token validado por el servicio:
/// el id del perfil autenticado, que los controladores pasan como entrada a su
/// caso de uso. La autorizacion (rol y perfil) vive en los atributos de
/// Infrastructure/Auth (RequireRole / RequireProfile).
/// </summary>
internal static class NotificationClaims
{
    internal static Guid CurrentProfileId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(SgClaimTypes.Subject), out var id) ? id : Guid.Empty;
}
