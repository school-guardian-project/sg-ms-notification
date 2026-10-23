using ms_notification.Api.Application.Dto.RouteDirectory;

namespace ms_notification.Api.Domain.Port.Out;


public interface IRouteDirectory
{
    Task<StudentRouteInfo?> GetStudentRouteAsync(Guid studentProfileId, CancellationToken ct = default);

    Task<StudentStopOnRoute?> GetStudentStopOnRouteAsync(
        Guid routeId, Guid studentProfileId, CancellationToken ct = default);

    Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default);

    Task<RouteInfo?> GetRouteByExecutionIdAsync(Guid routeExecutionId, CancellationToken ct = default);
}
