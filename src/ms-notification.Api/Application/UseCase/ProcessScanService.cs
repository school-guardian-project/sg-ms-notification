using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ms_notification.Api.Application.Configuration;
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
        _timeZone = ResolveTimeZone(_window.TimeZoneId, logger);
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

        EnsureWithinSchedule(route);

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

    /// <summary>
    /// El escaneo se acepta dentro de la jornada de la ruta con la holgura
    /// configurada, en la zona horaria de la operacion (Colombia). Se calcula
    /// sobre minutos del dia para cubrir de forma segura una ruta que cruza la
    /// medianoche (end_time menor que start_time).
    /// </summary>
    private void EnsureWithinSchedule(StudentRouteInfo route)
    {
        var localNow = TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), _timeZone);
        var nowMinutes = localNow.Hour * 60 + localNow.Minute;

        var startMinutes = route.StartTime.Hour * 60 + route.StartTime.Minute;
        var endMinutes = route.EndTime.Hour * 60 + route.EndTime.Minute;
        var margin = _window.MarginMinutes;

        // Sin jornada resoluble (TimeOnly.MinValue, dato inesperado de ms-route)
        // la ventana no restringe: la operacion del conductor no se bloquea por
        // un dato que no se pudo leer.
        if (startMinutes == 0 && endMinutes == 0)
        {
            return;
        }

        var opensAt = startMinutes - margin;
        var closesAt = endMinutes + margin;

        // Se compara en minutos del dia con aritmetica modular: cubre la ruta
        // normal (start < end) y la que cruza medianoche (end < start), ademas de
        // la holgura que empuja la ventana fuera del dia [0, 1440).
        var nowMod = Mod(nowMinutes);
        var from = Mod(opensAt);
        var to = Mod(closesAt);

        var within = from <= to
            ? nowMod >= from && nowMod <= to
            : nowMod >= from || nowMod <= to;

        if (!within)
        {
            throw new InvalidOperationException(
                "Scan is outside the route schedule window");
        }
    }

    private static int Mod(int minutes) => ((minutes % (24 * 60)) + 24 * 60) % (24 * 60);

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

    private static TimeZoneInfo ResolveTimeZone(string id, ILogger logger)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Colombia no tiene horario de verano: UTC-5 fijo como respaldo para
            // que una zona ausente no cambie las reglas de negocio.
            logger.LogWarning(ex, "Unknown time zone '{TimeZoneId}'; falling back to UTC-05:00", id);
            return TimeZoneInfo.CreateCustomTimeZone("scan-window", TimeSpan.FromHours(-5), "UTC-05:00", "UTC-05:00");
        }
    }

    private enum BoardingState
    {
        None,
        OnBoard,
        OffBoard
    }
}
