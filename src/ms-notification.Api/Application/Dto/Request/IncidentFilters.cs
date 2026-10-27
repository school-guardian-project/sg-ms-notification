using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto.Request;

/// <summary>
/// Filtros para listado de incidentes.
/// </summary>
public sealed class IncidentFilters
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string Sort { get; init; } = "reportedAt,desc";
    public IncidentType? IncidentType { get; init; }
    public IncidentSeverity? Severity { get; init; }
    public IncidentStatus? Status { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
}