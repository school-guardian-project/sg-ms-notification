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
        var notifications = await (
            from ar in _context.AlertRecipients
            join a in _context.Alerts on ar.AlertId equals a.Id
            where ar.ProfileId == profileId
            orderby ar.DateTimeRead descending
            select new GuardianNotificationDto
            {
                Id = ar.Id,
                AlertId = ar.AlertId,
                DateTimeRead = ar.DateTimeRead,
                Alert = new AlertDetailDto
                {
                    Id = a.Id,
                    Description = a.Description,
                    DateTime = a.DateTime,
                    AlertTypeId = a.AlertTypeId
                }
            })
            .ToListAsync(ct);

        return notifications;
    }
}
