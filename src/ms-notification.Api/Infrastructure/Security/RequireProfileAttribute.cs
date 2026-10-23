using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ms_notification.Api.Infrastructure.Security;

/// <summary>
/// Exige que la peticion incluya el claim "sub" (id del perfil autenticado que
/// inyecta el gateway) con un GUID valido y no vacio; si no responde 401 sin
/// cuerpo. Los endpoints que usan el perfil autenticado como entrada del caso
/// de uso (credential, guardian, devices, scan, incident report) lo declaran
/// aqui en lugar de comprobarlo a mano en el controlador.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireProfileAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
{
    /// <summary>Corre despues de <see cref="RequireRoleAttribute"/> (Order -20).</summary>
    public int Order => -10;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var profileId = context.HttpContext.User.FindFirstValue("sub");
        if (!Guid.TryParse(profileId, out var id) || id == Guid.Empty)
        {
            context.Result = new UnauthorizedResult();
        }

        return Task.CompletedTask;
    }
}
