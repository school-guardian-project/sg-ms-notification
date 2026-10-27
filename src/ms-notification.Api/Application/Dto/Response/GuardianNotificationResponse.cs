using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Response DTO for guardian notification.
/// </summary>
/// <param name="Id">Notification identifier.</param>
/// <param name="AlertId">Alert identifier.</param>
/// <param name="DateTimeRead">When the notification was read.</param>
/// <param name="BoardingType">Type of boarding.</param>
/// <param name="Alert">Alert detail.</param>
public sealed record GuardianNotificationResponse(
    Guid Id,
    Guid AlertId,
    DateTime DateTimeRead,
    BoardingType? BoardingType,
    AlertDetailResponse Alert);