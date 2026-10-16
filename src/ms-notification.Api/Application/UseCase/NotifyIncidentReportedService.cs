using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

/// <summary>
/// FR-8.2: entrega por push el incidente que reporto el conductor. Los
/// destinatarios (colegio + acudientes de los estudiantes a bordo) ya los resolvio
/// <see cref="ReportIncidentService"/> y viajan en el evento; aqui solo se
/// traducen a dispositivos y se empuja.
///
/// Es best-effort a proposito: si un acudiente no tiene dispositivo registrado,
/// o Expo falla, no se pierde nada: la alerta ya quedo en la base y se lee en la
/// app con GET /api/notifications/guardian.
/// </summary>
public class NotifyIncidentReportedService : INotifyIncidentReportedUseCase
{
    /// <summary>Titulo del push; la app lo muestra tal cual.</summary>
    public const string PushTitle = "Alerta de incidente";

    /// <summary>Cuerpo por defecto cuando el conductor no escribio una descripcion.</summary>
    public const string EmptyDescriptionBody = "El conductor reporto una emergencia durante el viaje";

    private readonly NotificationContext _context;
    private readonly IPushNotifier _pushNotifier;
    private readonly ILogger<NotifyIncidentReportedService> _logger;

    public NotifyIncidentReportedService(
        NotificationContext context,
        IPushNotifier pushNotifier,
        ILogger<NotifyIncidentReportedService> logger)
    {
        _context = context;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    public async Task NotifyAsync(IncidentReportedEvent incident, CancellationToken ct = default)
    {
        var recipients = incident.Recipients.Distinct().ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        var tokens = await _context.DeviceTokens
            .AsNoTracking()
            .Where(t => recipients.Contains(t.ProfileId))
            .Select(t => t.ExpoToken)
            .Distinct()
            .ToListAsync(ct);

        if (tokens.Count == 0)
        {
            _logger.LogInformation(
                "No device tokens for incident alert {AlertId}; it stays in-app", incident.AlertId);
            return;
        }

        var body = string.IsNullOrWhiteSpace(incident.Description)
            ? EmptyDescriptionBody
            : incident.Description;

        await _pushNotifier.SendAsync(tokens, PushTitle, body, ct);
    }
}
