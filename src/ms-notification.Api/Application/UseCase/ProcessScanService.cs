using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Application.Security;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Messaging.Kafka;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class ProcessScanService : IProcessScanUseCase
{
    private const int MaxDescriptionLength = 100;
    private const string DefaultStudentName = "Estudiante";

    // Ventana de idempotencia: un doble escaneo no debe crear dos abordajes.
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(30);

    private readonly NotificationContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IFamilyDirectory _familyDirectory;
    private readonly IStudentDirectory _studentDirectory;
    private readonly QrTokenSigner _signer;
    private readonly ILogger<ProcessScanService> _logger;

    public ProcessScanService(
        NotificationContext context,
        IEventPublisher eventPublisher,
        IRouteDirectory routeDirectory,
        IFamilyDirectory familyDirectory,
        IStudentDirectory studentDirectory,
        QrTokenSigner signer,
        ILogger<ProcessScanService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _routeDirectory = routeDirectory;
        _familyDirectory = familyDirectory;
        _studentDirectory = studentDirectory;
        _signer = signer;
        _logger = logger;
    }

    public async Task<ScanResponseDto> ExecuteAsync(
        Guid driverProfileId, ScanRequestDto request, CancellationToken ct = default)
    {
        if (request.BoardingType is not (BoardingType.ON_BOARD or BoardingType.OFF_BOARD))
        {
            throw new InvalidOperationException("Invalid boarding type");
        }

        var payload = _signer.Validate(request.QrToken)
                      ?? throw new InvalidOperationException("Invalid or expired QR credential");

        if (!Guid.TryParse(payload.StudentProfileId, out var studentProfileId))
        {
            throw new InvalidOperationException("Invalid QR credential");
        }

        // RouteExecutionId se resuelve del viaje ACTUAL del conductor, no del cliente.
        var trip = await _routeDirectory.GetCurrentTripAsync(driverProfileId, ct)
                   ?? throw new InvalidOperationException("Driver has no active trip");

        var route = await _routeDirectory.GetStudentRouteAsync(studentProfileId, ct)
                    ?? throw new InvalidOperationException("Student has no active route assigned");

        var stop = await _routeDirectory.GetStudentStopOnRouteAsync(route.RouteId, studentProfileId, ct)
                   ?? throw new InvalidOperationException("Student is not assigned to this route");

        if (request.BoardingType == BoardingType.OFF_BOARD)
        {
            var hasActiveBoarding = await _context.Boardings.AnyAsync(b =>
                b.ProfileId == studentProfileId &&
                b.RouteExecutionId == trip.Id &&
                b.BoardingType == BoardingType.ON_BOARD, ct);

            if (!hasActiveBoarding)
            {
                throw new InvalidOperationException("No active boarding for the student");
            }
        }

        // Idempotencia: mismo estudiante + viaje + tipo dentro de la ventana => ya existe.
        var since = DateTime.UtcNow.Subtract(DuplicateWindow);
        var duplicate = await _context.Boardings.AsNoTracking().FirstOrDefaultAsync(b =>
            b.ProfileId == studentProfileId &&
            b.RouteExecutionId == trip.Id &&
            b.BoardingType == request.BoardingType &&
            b.DateTime >= since, ct);

        if (duplicate is not null)
        {
            var existingAlert = await _context.Alerts.AsNoTracking().FirstOrDefaultAsync(a =>
                a.ProfileId == studentProfileId &&
                a.RouteExecutionId == trip.Id &&
                a.DateTime >= since, ct);

            return new ScanResponseDto
            {
                BoardingId = duplicate.Id,
                AlertId = existingAlert?.Id ?? Guid.Empty,
                ParentsNotified = await CountParentsAsync(studentProfileId, ct)
            };
        }

        var studentName = await _studentDirectory.GetStudentNameAsync(studentProfileId, ct);
        var description = BuildDescription(request.BoardingType, studentName, route.RouteName, stop.StopName);
        var now = DateTime.UtcNow;

        var boarding = new Boarding
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = trip.Id,
            ProfileId = studentProfileId,
            RouteStopId = stop.RouteStopId,
            DateTime = now,
            BoardingType = request.BoardingType
        };
        _context.Boardings.Add(boarding);

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = trip.Id,
            AlertTypeId = request.BoardingType == BoardingType.ON_BOARD ? (byte)1 : (byte)2,
            ProfileId = studentProfileId,
            DateTime = now,
            Description = description
        };
        _context.Alerts.Add(alert);

        await _context.SaveChangesAsync(ct);

        var eventData = StudentScannedEventMapper.ToEvent(
            boarding.Id, alert.Id, studentProfileId, trip.Id, request.BoardingType);

        await _eventPublisher.PublishAsync("student.scanned", eventData);

        return new ScanResponseDto
        {
            BoardingId = boarding.Id,
            AlertId = alert.Id,
            ParentsNotified = await CountParentsAsync(studentProfileId, ct)
        };
    }

    private static string BuildDescription(
        BoardingType boardingType, string? studentName, string? routeName, string? stopName)
    {
        var action = boardingType == BoardingType.OFF_BOARD ? "bajó del bus" : "subió al bus";
        var who = string.IsNullOrWhiteSpace(studentName) ? DefaultStudentName : studentName;
        var text = $"{who} {action}";

        if (!string.IsNullOrWhiteSpace(stopName))
        {
            text += $" en la parada {stopName}";
        }

        if (!string.IsNullOrWhiteSpace(routeName))
        {
            text += $" - {routeName}";
        }

        return text.Length > MaxDescriptionLength ? text[..MaxDescriptionLength] : text;
    }

    private async Task<int> CountParentsAsync(Guid studentProfileId, CancellationToken ct)
    {
        try
        {
            var parents = await _familyDirectory.GetParentProfileIdsAsync(studentProfileId, ct);
            return parents.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not count the family members of student {StudentProfileId}",
                studentProfileId);
            return 0;
        }
    }
}
