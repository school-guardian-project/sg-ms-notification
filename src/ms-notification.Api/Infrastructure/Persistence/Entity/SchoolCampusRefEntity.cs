namespace ms_notification.Api.Infrastructure.Persistence.Entity;

/// <summary>
/// Referencia de solo lectura a School.SchoolCampus (ms-school-management). Se
/// usa para llegar del campus del perfil al colegio (FR-8.2).
/// </summary>
public class SchoolCampusRefEntity
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
}
