using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Exige que el claim "roleId" que el gateway (Kong) inyecta tras validar el
/// access token este dentro de los roles permitidos. Es la comprobacion de rol
/// local del servicio: no confia en que el gateway ya haya autorizado.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
{
    public const string AdminRoleId = "1";
    public const string StudentRoleId = "2";
    public const string DriverRoleId = "3";
    public const string ParentRoleId = "4";
    public const string SuperAdminRoleId = "5";

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
