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
            BoardingTypeOf(alert.AlertTypeId),
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

    // 25/26 son los ids de escaneo del catalogo (007-insert-boarding-alert-types.sql).
    // El resto de ids no es un escaneo: el cast directo producia valores fuera
    // del enum (1 era 'Bus Delay', no ON_BOARD).
    private static ms_notification.Api.Domain.Enum.BoardingType BoardingTypeOf(byte alertTypeId)
        => alertTypeId switch
        {
            25 => ms_notification.Api.Domain.Enum.BoardingType.ON_BOARD,
            26 => ms_notification.Api.Domain.Enum.BoardingType.OFF_BOARD,
            _ => (ms_notification.Api.Domain.Enum.BoardingType)alertTypeId
        };
}