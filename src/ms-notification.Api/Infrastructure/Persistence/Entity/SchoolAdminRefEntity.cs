namespace ms_notification.Api.Infrastructure.Persistence.Entity;

/// <summary>
/// Referencia de solo lectura a School.SchoolAdmin (ms-school-management): los
/// administradores que deben enterarse de un incidente (FR-8.2).
/// </summary>
public class SchoolAdminRefEntity
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid ProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
}
