using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ms_notification.Api.Infrastructure.Security;

/// <summary>
/// Esquema de autenticacion que confia en las cabeceras X-User-* inyectadas por
/// el API gateway (Kong) tras validar el access token RS256. El gateway elimina
/// cualquier cabecera X-User-* entrante, por lo que el cliente no puede
/// suplantar su identidad. La peticion sigue siendo anonima cuando no hay
/// cabeceras (flujos internos sin token).
/// </summary>
public sealed class GatewayAuthenticationHandler : AuthenticationHandler<GatewayAuthenticationOptions>
{
    public const string SchemeName = "Gateway";

    public const string UserIdHeader = "X-User-Id";
    public const string UserRoleHeader = "X-User-Role";
    public const string UserRoleNameHeader = "X-User-Role-Name";
    public const string UserEmailHeader = "X-User-Email";
    public const string UserPermissionsHeader = "X-User-Permissions";
    public const string CampusIdHeader = "X-Campus-Id";
    public const string SchoolIdHeader = "X-School-Id";

    /// <summary>Tipo de claim que contiene cada permiso concedido (FR-2.1).</summary>
    public const string PermissionClaimType = "permission";

    public GatewayAuthenticationHandler(
        IOptionsMonitor<GatewayAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[UserIdHeader].ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.NameIdentifier, userId),
        };

        AddIfPresent(claims, "roleId", Request.Headers[UserRoleHeader].ToString());
        AddIfPresent(claims, ClaimTypes.Role, Request.Headers[UserRoleNameHeader].ToString());
        AddIfPresent(claims, "campusId", Request.Headers[CampusIdHeader].ToString());
        AddIfPresent(claims, "schoolId", Request.Headers[SchoolIdHeader].ToString());
        AddIfPresent(claims, "email", Request.Headers[UserEmailHeader].ToString());

        foreach (var permission in ReadPermissions(Request.Headers[UserPermissionsHeader].ToString()))
        {
            claims.Add(new Claim(PermissionClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static void AddIfPresent(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value));
        }
    }

    private static IEnumerable<string> ReadPermissions(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return Array.Empty<string>();
        }

        return header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
