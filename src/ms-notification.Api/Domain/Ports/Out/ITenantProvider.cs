namespace ms_notification.Api.Domain.Ports.Out;

/// <summary>
/// Expone el tenant (colegio/sede) del usuario autenticado según los claims del JWT.
/// Mismo contrato que ms-route y ms-fleet, para que JwtTenantProvider compile.
/// </summary>
public interface ITenantProvider
{
    int? RoleId { get; }
    Guid? CampusId { get; }
    Guid? SchoolId { get; }

    /// <summary>
    /// true cuando el token es válido y el rol exige acotar las rutas por tenant.
    /// Sin token, token inválido o SuperAdmin (roleId 5) => false.
    /// </summary>
    bool ShouldFilter { get; }
}
