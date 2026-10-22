namespace ms_notification.Api.Domain.Port.Out;


public interface IRouteDirectory
{
    Task<StudentRouteInfo?> GetStudentRouteAsync(Guid studentProfileId, CancellationToken ct = default);

    Task<StudentStopOnRoute?> GetStudentStopOnRouteAsync(
        Guid routeId, Guid studentProfileId, CancellationToken ct = default);

    Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default);

    Task<RouteInfo?> GetRouteByExecutionIdAsync(Guid routeExecutionId, CancellationToken ct = default);
}

// Ruta asignada del estudiante. Las horas viajan como HH:mm:ss desde ms-route y
// se exponen como TimeOnly. Con los schedules (dia + direccion + hora de salida)
// la ventana de escaneo se valida contra el dia operativo; StartTime/EndTime del
// par grueso quedan como respaldo cuando la ruta no tiene schedules.
public sealed record StudentRouteInfo(
    Guid RouteId,
    string RouteName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyList<StudentRouteSchedule> Schedules);

// Horario de la ruta: un dia de la semana, la direccion (Ida/Vuelta) y la hora
// de salida. ms-route lo envia como DayOfWeek.ToString() ("Monday", ...), que es
// el enum .NET; se parsea de vuelta al comparar contra el dia local de Colombia.
public sealed record StudentRouteSchedule(
    DayOfWeek DayOfWeek,
    string Direction,
    TimeOnly StartTime);

// Ubicacion de la parada asignada que ms-route resuelve en el escaneo (FR-5.2,
// FR-5.3). La direccion es la que se muestra al guardian; las coordenadas
// quedan en el evento para que el canal de envio pueda ubicarlas en un mapa.
public sealed record StudentStopOnRoute(
    Guid RouteStopId,
    Guid StopId,
    string StopName,
    string Address,
    double Latitude,
    double Longitude);

// RouteId permite comparar el viaje del conductor con la ruta del estudiante:
// sin el, un escaneo podia registrar en un viaje de otra ruta.
public sealed record CurrentTrip(Guid Id, Guid RouteId, Guid BusId, string Status);

// Información básica de la ruta para el dashboard.
public sealed record RouteInfo(
    Guid RouteId,
    string RouteName);