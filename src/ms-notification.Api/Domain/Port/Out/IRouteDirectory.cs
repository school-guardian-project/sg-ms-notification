namespace ms_notification.Api.Domain.Port.Out;


public interface IRouteDirectory
{
    Task<StudentRouteInfo?> GetStudentRouteAsync(Guid studentProfileId, CancellationToken ct = default);

    Task<StudentStopOnRoute?> GetStudentStopOnRouteAsync(
        Guid routeId, Guid studentProfileId, CancellationToken ct = default);

    Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default);
}

public sealed record StudentRouteInfo(Guid RouteId, string RouteName);

public sealed record StudentStopOnRoute(Guid RouteStopId, Guid StopId, string StopName);

public sealed record CurrentTrip(Guid Id, Guid BusId, string Status);