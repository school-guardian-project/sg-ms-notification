namespace ms_notification.Api.Domain.Port.Out;


public interface IRouteDirectory
{
    Task<StudentRouteInfo?> GetStudentRouteAsync(Guid studentProfileId, CancellationToken ct = default);

    Task<StudentStopOnRoute?> GetStudentStopOnRouteAsync(
        Guid routeId, Guid studentProfileId, CancellationToken ct = default);

    Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default);
}

public sealed record StudentRouteInfo(Guid RouteId, string RouteName);

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

public sealed record CurrentTrip(Guid Id, Guid BusId, string Status);