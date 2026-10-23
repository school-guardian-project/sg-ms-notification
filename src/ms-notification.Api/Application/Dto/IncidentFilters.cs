using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto;

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
