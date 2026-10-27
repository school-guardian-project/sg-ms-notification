using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Simplified incident for listings (paginated) in admin dashboard.
/// An incident is an alert of type INCIDENT/DELAY/BREAKDOWN/ACCIDENT/etc.
/// </summary>
/// <param name="Id">Incident identifier.</param>
/// <param name="RouteExecutionId">Route execution identifier.</param>
/// <param name="RouteName">Route name.</param>
/// <param name="DriverName">Driver name.</param>
/// <param name="BusPlate">Bus plate.</param>
/// <param name="IncidentType">Incident type.</param>
/// <param name="Severity">Incident severity.</param>
/// <param name="Description">Incident description.</param>
/// <param name="ReportedAt">When the incident was reported.</param>
/// <param name="ReportedBy">Who reported the incident.</param>
/// <param name="Status">Incident status.</param>
/// <param name="ResolvedAt">When the incident was resolved.</param>
/// <param name="ResolvedBy">Who resolved the incident.</param>
public sealed record IncidentListResponse(
    Guid Id,
    Guid RouteExecutionId,
    string RouteName,
    string DriverName,
    string BusPlate,
    IncidentType IncidentType,
    IncidentSeverity Severity,
    string Description,
    DateTime ReportedAt,
    string ReportedBy,
    IncidentStatus Status,
    DateTime? ResolvedAt,
    string? ResolvedBy);