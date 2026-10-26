using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class GetGuardianNotificationsService : IGetGuardianNotificationsUseCase
{
    private readonly NotificationContext _context;
    private readonly IGuardianNotificationMapper _notificationMapper;

    public GetGuardianNotificationsService(
        NotificationContext context,
        IGuardianNotificationMapper notificationMapper)
    {
        _context = context;
        _notificationMapper = notificationMapper;
    }

    public async Task<List<GuardianNotificationResponse>> ExecuteAsync(Guid profileId, CancellationToken ct = default)
    {
        var rows = await (
            from ar in _context.AlertRecipients
            join a in _context.Alerts on ar.AlertId equals a.Id
            where ar.ProfileId == profileId
            select new { Recipient = ar, Alert = a })
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return new List<GuardianNotificationResponse>();
        }

        var executionIds = rows.Select(r => r.Alert.RouteExecutionId).Distinct().ToList();
        var studentIds = rows.Select(r => r.Alert.ProfileId).Distinct().ToList();

        var boardings = await _context.Boardings
            .AsNoTracking()
            .Where(b => executionIds.Contains(b.RouteExecutionId) && studentIds.Contains(b.ProfileId))
            .ToListAsync(ct);

        return rows
            .Select(r =>
            {
                var boarding = boardings
                    .Where(b => b.ProfileId == r.Alert.ProfileId && b.RouteExecutionId == r.Alert.RouteExecutionId)
                    .OrderBy(b => Math.Abs((b.DateTime - r.Alert.DateTime).Ticks))
                    .FirstOrDefault();

                var alertDetail = new AlertDetailResponse(
                    r.Alert.Id,
                    r.Alert.Description,
                    boarding?.DateTime ?? r.Alert.DateTime,
                    r.Alert.AlertTypeId);

                var boardingType = boarding?.BoardingType;

                return _notificationMapper.ToResponse(r.Recipient, alertDetail, boardingType);
            })
            .OrderByDescending(n => n.Alert.DateTime)
            .ToList();
    }
}