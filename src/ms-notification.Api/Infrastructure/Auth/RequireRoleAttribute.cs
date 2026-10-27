using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Exige que el claim "roleId" del access token este dentro de los roles
/// permitidos. Responde 401 sin token valido y 403 sin cuerpo si el rol falta
/// o no coincide, igual que la logica que los controladores hacian a mano.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
{
    internal const string AdminRoleId = "1";
    internal const string StudentRoleId = "2";
    internal const string DriverRoleId = "3";
    internal const string ParentRoleId = "4";
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
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        var roleId = user.FindFirstValue(SgClaimTypes.RoleId);
        if (!Array.Exists(_allowedRoleIds, id => id == roleId))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        return Task.CompletedTask;
    }
}
