using ms_notification.Api.Application.Dto.RouteDirectory;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Tests.Fakes;

/// <summary>
/// Stand-in for ms-route: every answer is set by the test, and a null means the
/// directory could not resolve the lookup (the reason ProcessScanService rejects).
/// </summary>
internal sealed class FakeRouteDirectory : IRouteDirectory
{
    public StudentRouteInfo? StudentRoute { get; set; }
    public StudentStopOnRoute? StudentStopOnRoute { get; set; }
    public CurrentTrip? CurrentTrip { get; set; }
    public RouteInfo? RouteByExecutionId { get; set; }

    public Task<StudentRouteInfo?> GetStudentRouteAsync(
        Guid studentProfileId, CancellationToken ct = default)
        => Task.FromResult(StudentRoute);

    public Task<StudentStopOnRoute?> GetStudentStopOnRouteAsync(
        Guid routeId, Guid studentProfileId, CancellationToken ct = default)
        => Task.FromResult(StudentStopOnRoute);

    public Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default)
        => Task.FromResult(CurrentTrip);

    public Task<RouteInfo?> GetRouteByExecutionIdAsync(Guid routeExecutionId, CancellationToken ct = default)
        => Task.FromResult(RouteByExecutionId);
}
