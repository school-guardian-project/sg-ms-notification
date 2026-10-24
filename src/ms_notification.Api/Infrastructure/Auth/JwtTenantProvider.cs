using System.Security.Claims;
using ms_notification.Api.Domain.Ports.Out;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Lee el tenant desde HttpContext.User (claims roleId/campusId/schoolId emitidos por ms-iam).
/// Sin token o token inválido => no se filtra (compatibilidad con llamadas internas y app móvil).
/// </summary>
public class JwtTenantProvider : ITenantProvider
{
    private const int RoleAdmin = 1;
    private const int RoleSuperAdmin = 5;

    private readonly ClaimsPrincipal? _user;

    public JwtTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;
    }

    public int? RoleId => ParseInt(GetClaim("roleId"));

    public Guid? CampusId => ParseGuid(GetClaim("campusId"));

    public Guid? SchoolId => ParseGuid(GetClaim("schoolId"));

    public bool ShouldFilter => RoleId switch
    {
        null => false,
        RoleSuperAdmin => false,
        RoleAdmin => SchoolId.HasValue,
        >= 2 and <= 4 => CampusId.HasValue,
        _ => false
    };

    private string? GetClaim(string type) =>
        _user is { Identity.IsAuthenticated: true }
            ? _user.FindFirst(type)?.Value
            : null;

    private static int? ParseInt(string? value) =>
        int.TryParse(value, out var result) ? result : null;

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var result) ? result : null;
}
