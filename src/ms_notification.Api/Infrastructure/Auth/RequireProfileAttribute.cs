using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Exige un perfil autenticado (claim "sub" con GUID valido, inyectado por el
/// gateway). Responde 401 cuando la peticion no trae identidad.
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
