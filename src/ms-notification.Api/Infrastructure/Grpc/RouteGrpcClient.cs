using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Api.Infrastructure.Grpc;

public sealed class RouteGrpcClient : IRouteDirectory, IDisposable
{
    // Holgura amplia: la primera llamada tras un arranque en frio compila el modelo
    // de EF en el servicio destino y puede tardar varios segundos.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly GrpcChannel _channel;
    private readonly RouteService.RouteServiceClient _client;
    private readonly ILogger<RouteGrpcClient> _logger;

    public RouteGrpcClient(IConfiguration configuration, ILogger<RouteGrpcClient> logger)
    {
        _channel = GrpcChannel.ForAddress(
            configuration["Grpc:Route:Address"]
            ?? throw new InvalidOperationException("Missing Grpc__Route__Address (set it in .env)"));
        _client = new RouteService.RouteServiceClient(_channel);
        _logger = logger;
    }

    public async Task<StudentRouteInfo?> GetStudentRouteAsync(
        Guid studentProfileId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetStudentRouteAsync(
                new GetStudentRouteRequest { StudentId = studentProfileId.ToString() },
                deadline: DateTime.UtcNow.Add(Timeout),
                cancellationToken: ct);

            return new StudentRouteInfo(
                Guid.Parse(response.Id),
                response.Name,
                ParseTime(response.StartTime),
                ParseTime(response.EndTime),
                ParseSchedules(response.Schedules));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<StudentStopOnRoute?> GetStudentStopOnRouteAsync(
        Guid routeId, Guid studentProfileId, CancellationToken ct = default)
    {
        var response = await _client.GetStudentStopOnRouteAsync(
            new GetStudentStopOnRouteRequest
            {
                RouteId = routeId.ToString(),
                StudentProfileId = studentProfileId.ToString()
            },
            deadline: DateTime.UtcNow.Add(Timeout),
            cancellationToken: ct);

        return response.Found
            ? new StudentStopOnRoute(
                Guid.Parse(response.RouteStopId),
                Guid.Parse(response.StopId),
                response.StopName,
                response.StopAddress,
                response.Latitude,
                response.Longitude)
            : null;
    }

    public async Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default)
    {
        var response = await _client.GetCurrentTripAsync(
            new GetCurrentTripRequest { DriverId = driverId.ToString() },
            deadline: DateTime.UtcNow.Add(Timeout),
            cancellationToken: ct);

        return response.Found
            ? new CurrentTrip(
                Guid.Parse(response.Id),
                ParseId(response.RouteId, nameof(response.RouteId)),
                Guid.Parse(response.BusId),
                response.Status)
            : null;
    }

    public async Task<RouteInfo?> GetRouteByExecutionIdAsync(Guid routeExecutionId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetRouteByExecutionIdAsync(
                new GetRouteByExecutionIdRequest { ExecutionId = routeExecutionId.ToString() },
                deadline: DateTime.UtcNow.Add(Timeout),
                cancellationToken: ct);

            return response.Found
                ? new RouteInfo(Guid.Parse(response.RouteId), response.RouteName)
                : null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// HH:mm:ss desde ms-route. Si el valor falta o no parsea la ventana horaria
    /// queda abierta (TimeOnly.MinValue..MaxValue) en vez de bloquear la operacion
    /// del conductor por un dato inesperado.
    /// </summary>
    private TimeOnly ParseTime(string value)
    {
        if (TimeOnly.TryParse(value, out var time))
        {
            return time;
        }

        _logger.LogWarning("ms-route returned an unparseable time '{Value}'; the scan window stays open", value);
        return TimeOnly.MinValue;
    }

    /// <summary>
    /// Horarios por dia (Ida/Vuelta). Un schedule que no parsea se descarta con
    /// warning: no vale la pena tumbar el escaneo por una fila corrupta, y al
    /// final siempre queda el respaldo del par grueso start_time/end_time.
    /// </summary>
    private IReadOnlyList<StudentRouteSchedule> ParseSchedules(
        IEnumerable<ScheduleItem> schedules)
    {
        var parsed = new List<StudentRouteSchedule>();

        foreach (var schedule in schedules)
        {
            if (!Enum.TryParse<DayOfWeek>(schedule.DayOfWeek, ignoreCase: true, out var day))
            {
                _logger.LogWarning(
                    "ms-route returned an unparseable day_of_week '{Value}'; the schedule is skipped",
                    schedule.DayOfWeek);
                continue;
            }

            if (!TimeOnly.TryParse(schedule.StartTime, out var start))
            {
                _logger.LogWarning(
                    "ms-route returned an unparseable schedule start_time '{Value}'; the schedule is skipped",
                    schedule.StartTime);
                continue;
            }

            parsed.Add(new StudentRouteSchedule(day, schedule.Direction, start));
        }

        return parsed;
    }

    private Guid ParseId(string value, string field)
    {
        if (Guid.TryParse(value, out var id))
        {
            return id;
        }

        _logger.LogWarning("ms-route returned an invalid {Field} '{Value}'", field, value);
        return Guid.Empty;
    }

    public void Dispose() => _channel.Dispose();
}