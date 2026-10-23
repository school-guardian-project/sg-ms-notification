using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ms_notification.Api.Infrastructure.Security;

/// <summary>
/// Exige que el claim "roleId" que el gateway (Kong) inyecta tras validar el
/// token este dentro de los roles permitidos: 1 admin, 3 driver, 5 superadmin.
/// Si el rol falta o no coincide responde 403 sin cuerpo, igual que la logica
/// que los controladores hacian a mano.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
{
    internal const string AdminRoleId = "1";
    internal const string DriverRoleId = "3";
    internal const string SuperAdminRoleId = "5";

    private readonly string[] _allowedRoleIds;

    /// <summary>Corre antes que <see cref="RequireProfileAttribute"/> (Order -10).</summary>
    public int Order => -20;

    public RequireRoleAttribute(params string[] allowedRoleIds)
    {
        _allowedRoleIds = allowedRoleIds;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var roleId = context.HttpContext.User.FindFirstValue("roleId");
        if (!Array.Exists(_allowedRoleIds, id => id == roleId))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        return Task.CompletedTask;
    }
}
