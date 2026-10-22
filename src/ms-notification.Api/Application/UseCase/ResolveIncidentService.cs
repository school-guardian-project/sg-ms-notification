using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class ResolveIncidentService : IResolveIncidentUseCase
{
    private readonly NotificationContext _context;

    public ResolveIncidentService(NotificationContext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(Guid incidentId, CancellationToken ct = default)
    {
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.Id == incidentId, ct);

        if (alert == null)
        {
            throw new InvalidOperationException($"Incident not found: {incidentId}");
        }

        // Verificar que sea un incidente (no boarding/alighting)
        var incidentAlertTypeIds = new byte[] { 1, 2, 3, 4, 11, 13, 21, 23 };
        if (!incidentAlertTypeIds.Contains(alert.AlertTypeId))
        {
            throw new InvalidOperationException("Alert is not an incident");
        }

        // TODO: agregar campo Status a Alert cuando se haga la migración
        // Por ahora solo registramos la resolución en AcknowledgedAt si no estaba
        if (!alert.AcknowledgedAt.HasValue)
        {
            alert.AcknowledgedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
        // TODO: cuando se agregue campo Status, actualizar a Resolved/Closed
    }
}