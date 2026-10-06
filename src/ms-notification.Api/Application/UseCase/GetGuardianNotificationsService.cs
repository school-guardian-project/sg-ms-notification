using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class GetGuardianNotificationsService
{
    private readonly NotificationContext _context;

    public GetGuardianNotificationsService(NotificationContext context)
    {
        _context = context;
    }

    public async Task<List<GuardianNotificationDto>> ExecuteAsync(Guid profileId, CancellationToken ct = default)
    {
        var rows = await (
            from ar in _context.AlertRecipients
            join a in _context.Alerts on ar.AlertId equals a.Id
            where ar.ProfileId == profileId
            select new { Recipient = ar, Alert = a })
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return new List<GuardianNotificationDto>();
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

                return new GuardianNotificationDto
                {
                    Id = r.Recipient.Id,
                    AlertId = r.Alert.Id,
                    DateTimeRead = r.Recipient.DateTimeRead,
                    BoardingType = boarding?.BoardingType,
                    Alert = new AlertDetailDto
                    {
                        Id = r.Alert.Id,
                        Description = r.Alert.Description,
                        DateTime = boarding?.DateTime ?? r.Alert.DateTime,
                        AlertTypeId = r.Alert.AlertTypeId
                    }
                };
            })
            .OrderByDescending(n => n.Alert.DateTime)
            .ToList();
    }
}
