using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Dto.RouteDirectory;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

internal static class AlertListDtoMapper
{
    public static AlertListDto ToDto(
        Alert alert,
        StudentInfo? student,
        RouteInfo? route)
    {
        return new AlertListDto
        {
            Id = alert.Id,
            RouteExecutionId = alert.RouteExecutionId,
            ProfileId = alert.ProfileId,
            StudentName = student?.FullName ?? "Desconocido",
            RouteName = route?.RouteName ?? "Sin ruta",
            AlertType = (BoardingType)alert.AlertTypeId,
            Message = alert.Description ?? string.Empty,
            Timestamp = alert.DateTime,
            Acknowledged = alert.AcknowledgedAt.HasValue,
            AcknowledgedAt = alert.AcknowledgedAt,
            AcknowledgedBy = null
        };
    }
}
