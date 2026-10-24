using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Exige que la peticion incluya el permiso indicado (FR-2.1). El permiso viaja
/// como cabecera X-User-Permissions que el gateway deriva del claim
/// permissions[] del access token.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        if (!user.HasClaim(GatewayAuthenticationHandler.PermissionClaimType, _permission))
        {
            context.Result = new ObjectResult(new { message = "Forbidden" })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }

        return Task.CompletedTask;
    }
}
