using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Dto.RouteDirectory;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

/// <summary>
/// Maps Alert domain entities to Response DTOs.
/// This mapper is stateless and can be registered as a singleton.
/// </summary>
public interface IAlertMapper
{
    AlertListResponse ToListResponse(Alert alert, StudentInfo? student, RouteInfo? route);
    AlertDetailResponse ToDetailResponse(Alert alert);
}

public sealed class AlertMapper : IAlertMapper
{
    public AlertListResponse ToListResponse(Alert alert, StudentInfo? student, RouteInfo? route)
    {
        return new AlertListResponse(
            alert.Id,
            alert.RouteExecutionId,
            alert.ProfileId,
            student?.FullName ?? "Desconocido",
            route?.RouteName ?? "Sin ruta",
            (ms_notification.Api.Domain.Enum.BoardingType)alert.AlertTypeId,
            alert.Description ?? string.Empty,
            alert.DateTime,
            alert.AcknowledgedAt.HasValue,
            alert.AcknowledgedAt,
            null);
    }

    public AlertDetailResponse ToDetailResponse(Alert alert)
    {
        return new AlertDetailResponse(
            alert.Id,
            alert.Description,
            alert.DateTime,
            alert.AlertTypeId);
    }
}