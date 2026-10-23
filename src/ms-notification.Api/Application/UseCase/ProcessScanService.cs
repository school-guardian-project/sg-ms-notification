using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ms_notification.Api.Application.Configuration;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Application.Security;
using ms_notification.Api.Application.Service;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class ProcessScanService : IProcessScanUseCase
{
    private readonly NotificationContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IFamilyDirectory _familyDirectory;
    private readonly IStudentDirectory _studentDirectory;
    private readonly QrTokenSigner _signer;
    private readonly ScanWindowOptions _window;
    private readonly TimeZoneInfo _timeZone;
    private readonly TimeProvider _clock;
    private readonly ILogger<ProcessScanService> _logger;

    public ProcessScanService(
        NotificationContext context,
        IEventPublisher eventPublisher,
        IRouteDirectory routeDirectory,
        IFamilyDirectory familyDirectory,
        IStudentDirectory studentDirectory,
        QrTokenSigner signer,
        IOptions<ScanWindowOptions> window,
        TimeProvider clock,
        ILogger<ProcessScanService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _routeDirectory = routeDirectory;
        _familyDirectory = familyDirectory;
        _studentDirectory = studentDirectory;
        _signer = signer;
        _window = window.Value;
        _timeZone = ScanScheduleRule.ResolveTimeZone(_window.TimeZoneId, logger);
        _clock = clock;
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

        // El estudiante debe pertenecer a la ruta del viaje en curso: sin esta
        // comparacion un QR podia registrarse contra la ruta de otro conductor.
        if (trip.RouteId == Guid.Empty || route.RouteId != trip.RouteId)
        {
            throw new InvalidOperationException("Student does not belong to this route");
        }

        var stop = await _routeDirectory.GetStudentStopOnRouteAsync(route.RouteId, studentProfileId, ct)
                   ?? throw new InvalidOperationException("Student is not assigned to this route");

        ScanScheduleRule.EnsureWithinSchedule(route, _timeZone, _clock, _window);

        var state = await CurrentStateAsync(studentProfileId, trip.Id, ct);

        // Un escaneo repetido en el mismo estado no se vuelve a registrar ni a
        // notificar: se responde con lo ya existente (idempotencia por estado,
        // reemplaza la ventana fija de 30 segundos, que confundia un reingreso
        // legitimo con un doble toque).
        //
        // La tabla de estados del escaneo:
        //   Nunca abordo  + ON_BOARD  -> registra
        //   Nunca abordo  + OFF_BOARD -> rechaza (no hay de que bajarse)
        //   A bordo       + ON_BOARD  -> idempotente
        //   A bordo       + OFF_BOARD -> registra
        //   Ya descendio  + ON_BOARD  -> registra (reingreso legitimo)
        //   Ya descendio  + OFF_BOARD -> idempotente
        if (state == BoardingState.OnBoard && request.BoardingType == BoardingType.ON_BOARD)
        {
            return await ExistingResponseAsync(
                studentProfileId, trip.Id, BoardingType.ON_BOARD, ct);
        }

        if (state == BoardingState.OffBoard && request.BoardingType == BoardingType.OFF_BOARD)
        {
            return await ExistingResponseAsync(
                studentProfileId, trip.Id, BoardingType.OFF_BOARD, ct);
        }

        if (state == BoardingState.None && request.BoardingType == BoardingType.OFF_BOARD)
        {
            // No se desciende de un bus en el que nunca se abordo.
            throw new InvalidOperationException("No active boarding for the student");
        }


        var studentName = await _studentDirectory.GetStudentNameAsync(studentProfileId, ct);
        var description = AlertDescriptionBuilder.Build(
            request.BoardingType, studentName, route.RouteName, stop.StopName, stop.Address);
        var now = _clock.GetUtcNow().UtcDateTime;

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
            boarding.Id, alert.Id, studentProfileId, trip.Id, request.BoardingType, stop);

        await _eventPublisher.PublishAsync("student.scanned", eventData);

        return new ScanResponseDto
        {
            BoardingId = boarding.Id,
            AlertId = alert.Id,
            ParentsNotified = await CountParentsAsync(studentProfileId, ct)
        };
    }

    private async Task<BoardingState> CurrentStateAsync(
        Guid studentProfileId, Guid tripId, CancellationToken ct)
    {
        var last = await _context.Boardings
            .AsNoTracking()
            .Where(b => b.ProfileId == studentProfileId && b.RouteExecutionId == tripId)
            // Desempate por tipo: si un abordaje y un descenso comparten instante
            // (relojes con resolucion gruesa), el descenso es el estado vigente.
            .OrderByDescending(b => b.DateTime)
            .ThenByDescending(b => b.BoardingType)
            .FirstOrDefaultAsync(ct);

        if (last is null)
        {
            return BoardingState.None;
        }

        return last.BoardingType == BoardingType.ON_BOARD
            ? BoardingState.OnBoard
            : BoardingState.OffBoard;
    }

    private async Task<ScanResponseDto> ExistingResponseAsync(
        Guid studentProfileId, Guid tripId, BoardingType type, CancellationToken ct)
    {
        var boarding = await _context.Boardings
            .AsNoTracking()
            .Where(b => b.ProfileId == studentProfileId &&
                        b.RouteExecutionId == tripId &&
                        b.BoardingType == type)
            .OrderByDescending(b => b.DateTime)
            .ThenByDescending(b => b.BoardingType)
            .FirstAsync(ct);

        var alertTypeId = type == BoardingType.ON_BOARD ? (byte)1 : (byte)2;
        var alert = await _context.Alerts
            .AsNoTracking()
            .Where(a => a.ProfileId == studentProfileId &&
                        a.RouteExecutionId == tripId &&
                        a.AlertTypeId == alertTypeId)
            .OrderByDescending(a => a.DateTime)
            .FirstOrDefaultAsync(ct);

        return new ScanResponseDto
        {
            BoardingId = boarding.Id,
            AlertId = alert?.Id ?? Guid.Empty,
            ParentsNotified = await CountParentsAsync(studentProfileId, ct),
            AlreadyProcessed = true
        };
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

    private enum BoardingState
    {
        None,
        OnBoard,
        OffBoard
    }
}
