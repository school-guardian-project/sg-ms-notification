using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class AcknowledgeAlertService : IAcknowledgeAlertUseCase
{
    private readonly NotificationContext _context;
    private readonly IStudentDirectory _studentDirectory;

    public AcknowledgeAlertService(
        NotificationContext context,
        IStudentDirectory studentDirectory)
    {
        _context = context;
        _studentDirectory = studentDirectory;
    }

    public async Task ExecuteAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.Id == alertId, ct);

        if (alert == null)
        {
            throw new InvalidOperationException($"Alert not found: {alertId}");
        }

        if (alert.AcknowledgedAt.HasValue)
        {
            throw new InvalidOperationException("Alert already acknowledged");
        }

        alert.AcknowledgedAt = DateTime.UtcNow;
        // TODO: guardar quién reconoció (desde claims del JWT)

        await _context.SaveChangesAsync(ct);
    }
}