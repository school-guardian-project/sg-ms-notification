using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

/// <summary>
/// FR-8.1 (el conductor reporta un incidente o emergencia durante el viaje) y
/// FR-8.2 (se avisa al colegio y a los acudientes de los estudiantes a bordo).
///
/// El reporte se modela con la entidad Alert (AlertType / Alert / AlertRecipient),
/// como ya especifica el requisito; no hay tabla propia de incidentes.
/// </summary>
public class ReportIncidentService : IReportIncidentUseCase
{
    /// <summary>Limite de Notification.Alert.Description (ensanchada a 500 en FR-8.1).</summary>
    public const int MaxDescriptionLength = 500;

    /// <summary>Topic del evento que anuncia el incidente (HU-INCIDENT-001).</summary>
    public const string IncidentReportedTopic = "incident.reported";

    private readonly NotificationContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IFamilyDirectory _familyDirectory;
    private readonly ISchoolDirectory _schoolDirectory;
    private readonly ILogger<ReportIncidentService> _logger;

    public ReportIncidentService(
        NotificationContext context,
        IEventPublisher eventPublisher,
        IRouteDirectory routeDirectory,
        IFamilyDirectory familyDirectory,
        ISchoolDirectory schoolDirectory,
        ILogger<ReportIncidentService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _routeDirectory = routeDirectory;
        _familyDirectory = familyDirectory;
        _schoolDirectory = schoolDirectory;
        _logger = logger;
    }

    public async Task<IncidentResponseDto> ExecuteAsync(
        Guid driverProfileId, ReportIncidentRequestDto request, CancellationToken ct = default)
    {
        var description = request.Description?.Trim();
        if (description is { Length: > MaxDescriptionLength })
        {
            throw new InvalidOperationException("Incident description is too long");
        }

        // El tipo sale del catalogo AlertType: no se acepta un id inventado.
        var typeExists = await _context.AlertTypes.AnyAsync(t => t.Id == request.AlertTypeId, ct);
        if (!typeExists)
        {
            throw new InvalidOperationException("Unknown incident type");
        }

        // El viaje es obligatorio y lo resuelve el servidor a partir del conductor
        // autenticado, nunca el cliente (escenario 2 de la HU).
        var trip = await _routeDirectory.GetCurrentTripAsync(driverProfileId, ct)
                   ?? throw new InvalidOperationException("Driver has no active trip");

        var studentsOnBoard = await GetStudentsOnBoardAsync(trip.Id, ct);
        var recipients = await ResolveRecipientsAsync(driverProfileId, studentsOnBoard, ct);
        var now = DateTime.UtcNow;

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = trip.Id,
            AlertTypeId = request.AlertTypeId,
            ProfileId = driverProfileId,
            DateTime = now,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        };
        _context.Alerts.Add(alert);

        // Cada destinatario recibe su fila: es lo que alimenta el listado del
        // acudiente (GET /notifications/guardian) y la entrega por push.
        foreach (var recipientId in recipients)
        {
            _context.AlertRecipients.Add(new AlertRecipient
            {
                Id = Guid.NewGuid(),
                AlertId = alert.Id,
                ProfileId = recipientId,
                DateTimeRead = now
            });
        }

        await _context.SaveChangesAsync(ct);

        await _eventPublisher.PublishAsync(
            IncidentReportedTopic,
            IncidentReportedEventMapper.ToEvent(alert, studentsOnBoard, recipients));

        return new IncidentResponseDto
        {
            AlertId = alert.Id,
            RouteExecutionId = trip.Id,
            StudentsOnBoard = studentsOnBoard.Count,
            RecipientsNotified = recipients.Count
        };
    }

    /// <summary>
    /// Estudiantes a bordo: los que tienen su ultimo registro del viaje como
    /// abordaje. Un estudiante que ya bajo del bus no cuenta para el incidente.
    /// </summary>
    private async Task<List<Guid>> GetStudentsOnBoardAsync(
        Guid routeExecutionId, CancellationToken ct)
    {
        var boardings = await _context.Boardings
            .AsNoTracking()
            .Where(b => b.RouteExecutionId == routeExecutionId)
            .Select(b => new { b.ProfileId, b.DateTime, b.BoardingType })
            .ToListAsync(ct);

        return boardings
            .GroupBy(b => b.ProfileId)
            .Where(g => g.OrderByDescending(b => b.DateTime).First().BoardingType == BoardingType.ON_BOARD)
            .Select(g => g.Key)
            .ToList();
    }

    /// <summary>
    /// FR-8.2: colegio (administradores del campus del conductor) y acudientes de
    /// los estudiantes a bordo. Es best-effort a proposito: si un directorio
    /// externo falla, el incidente se registra igual y se avisa a quien si se pudo.
    /// </summary>
    private async Task<List<Guid>> ResolveRecipientsAsync(
        Guid driverProfileId, List<Guid> studentsOnBoard, CancellationToken ct)
    {
        var recipients = new List<Guid>();

        try
        {
            recipients.AddRange(await _schoolDirectory.GetSchoolAdminProfileIdsAsync(driverProfileId, ct));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve the school admins for driver {DriverProfileId}",
                driverProfileId);
        }

        foreach (var studentProfileId in studentsOnBoard)
        {
            try
            {
                recipients.AddRange(await _familyDirectory.GetParentProfileIdsAsync(studentProfileId, ct));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve the family of student {StudentProfileId}",
                    studentProfileId);
            }
        }

        // Un mismo acudiente con dos hijos a bordo recibe una sola alerta, y el
        // conductor que reporta no se notifica a si mismo.
        return recipients.Distinct().Where(id => id != driverProfileId).ToList();
    }
}
