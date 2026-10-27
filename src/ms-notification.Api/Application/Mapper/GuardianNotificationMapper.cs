using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

/// <summary>
/// Maps AlertRecipient domain entities to Response DTOs.
/// This mapper is stateless and can be registered as a singleton.
/// </summary>
public interface IGuardianNotificationMapper
{
    GuardianNotificationResponse ToResponse(
        AlertRecipient recipient,
        AlertDetailResponse alertDetail,
        BoardingType? boardingType);
}

public sealed class GuardianNotificationMapper : IGuardianNotificationMapper
{
    public GuardianNotificationResponse ToResponse(
        AlertRecipient recipient,
        AlertDetailResponse alertDetail,
        BoardingType? boardingType)
    {
        return new GuardianNotificationResponse(
            recipient.Id,
            recipient.AlertId,
            recipient.DateTimeRead,
            boardingType,
            alertDetail);
    }
}