namespace ms_notification.Api.Domain.Port.Out;


public interface IRouteDirectory
{
    Task<StudentRouteInfo?> GetStudentRouteAsync(Guid studentProfileId, CancellationToken ct = default);

    Task<StudentStopOnRoute?> GetStudentStopOnRouteAsync(
        Guid routeId, Guid studentProfileId, CancellationToken ct = default);

    Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default);
}

// Ruta asignada del estudiante. Las horas viajan como HH:mm:ss desde ms-route y
// se exponen como TimeOnly: son la ventana horaria que valida el escaneo.
public sealed record StudentRouteInfo(
    Guid RouteId,
    string RouteName,
    TimeOnly StartTime,
    TimeOnly EndTime);

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