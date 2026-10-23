using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto;

/// <summary>
/// Incidente simplificado para listados (paginados) en el dashboard admin.
/// Un incidente es una alerta de tipo INCIDENT/DELAY/BREAKDOWN/ACCIDENT/etc.
/// </summary>
public class IncidentListDto
{
    public Guid Id { get; set; }
    public Guid RouteExecutionId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string BusPlate { get; set; } = string.Empty;
    public IncidentType IncidentType { get; set; }
    public IncidentSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}
