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

/// <summary>
/// Respuesta paginada de incidentes.
/// </summary>
public class IncidentsResponse
{
    public IncidentListDto[] Content { get; set; } = Array.Empty<IncidentListDto>();
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public int Size { get; set; }
    public int Number { get; set; }
}

/// <summary>
/// Filtros para listado de incidentes.
/// </summary>
public class IncidentFilters
{
    public int Page { get; set; } = 0;
    public int Size { get; set; } = 10;
    public string Sort { get; set; } = "reportedAt,desc";
    public IncidentType? IncidentType { get; set; }
    public IncidentSeverity? Severity { get; set; }
    public IncidentStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }
}