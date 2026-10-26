using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

/// <summary>
/// Maps GuardianNotification domain entities to Response DTOs.
/// This mapper is stateless and can be registered as a singleton.
/// </summary>
public interface IGuardianNotificationMapper
{
    GuardianNotificationResponse ToResponse(
        Domain.Model.GuardianNotification notification,
        AlertDetailResponse alertDetail);
}

public sealed class GuardianNotificationMapper : IGuardianNotificationMapper
{
    public GuardianNotificationResponse ToResponse(
        Domain.Model.GuardianNotification notification,
        AlertDetailResponse alertDetail)
    {
        return new GuardianNotificationResponse(
            notification.Id,
            notification.AlertId,
            notification.DateTimeRead,
            (ms_notification.Api.Domain.Enum.BoardingType?)notification.BoardingType,
            alertDetail);
    }
}