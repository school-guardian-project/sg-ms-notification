using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

/// <summary>
/// Maps Incident (Alert) domain entities to Response DTOs.
/// This mapper is stateless and can be registered as a singleton.
/// </summary>
public interface IIncidentMapper
{
    IncidentListResponse ToListResponse(
        Alert alert,
        string routeName,
        string driverName,
        string busPlate,
        string reportedBy,
        IncidentStatus status,
        DateTime? resolvedAt,
        string? resolvedBy);
    IncidentResponse ToResponse(Alert alert, int studentsOnBoard, int recipientsNotified);
}

public sealed class IncidentMapper : IIncidentMapper
{
    public IncidentListResponse ToListResponse(
        Alert alert,
        string routeName,
        string driverName,
        string busPlate,
        string reportedBy,
        IncidentStatus status,
        DateTime? resolvedAt,
        string? resolvedBy)
    {
        return new IncidentListResponse(
            alert.Id,
            alert.RouteExecutionId,
            routeName,
            driverName,
            busPlate,
            IncidentTypeMapper.Map(alert.AlertTypeId),
            IncidentSeverityMapper.Map(alert.AlertTypeId),
            alert.Description ?? string.Empty,
            alert.DateTime,
            reportedBy,
            status,
            resolvedAt,
            resolvedBy);
    }

    public IncidentResponse ToResponse(Alert alert, int studentsOnBoard, int recipientsNotified)
    {
        return new IncidentResponse(
            alert.Id,
            alert.RouteExecutionId,
            studentsOnBoard,
            recipientsNotified);
    }
}