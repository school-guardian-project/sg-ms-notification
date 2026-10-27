using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Infrastructure.Persistence.Context;
using static ms_notification.Api.Infrastructure.Grpc.BoardingGrpcMapper;

namespace ms_notification.Api.Infrastructure.Grpc;

/// <summary>
/// Implementación del servicio gRPC BoardingService para consultas de conteo
/// de embarques/desembarques. Expone datos de la tabla Notification.Boarding
/// para que ms-route pueda determinar condiciones de fin de viaje.
/// </summary>
public sealed class BoardingGrpcService : BoardingService.BoardingServiceBase
{
    private readonly NotificationContext _context;
    private readonly ILogger<BoardingGrpcService> _logger;

    public BoardingGrpcService(NotificationContext context, ILogger<BoardingGrpcService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<GetAlightedCountAtStopResponse> GetAlightedCountAtStop(
        GetAlightedCountAtStopRequest request, ServerCallContext context)
    {
        try
        {
            var tripId = ParseId(request.TripId, "trip_id");
            var routeStopId = ParseId(request.RouteStopId, "route_stop_id");

            var count = await _context.Boardings
                .AsNoTracking()
                .Where(b => b.RouteExecutionId == tripId
                         && b.RouteStopId == routeStopId
                         && b.BoardingType == BoardingType.OFF_BOARD)
                .CountAsync(context.CancellationToken);

            return new GetAlightedCountAtStopResponse { Count = count };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying alighted count for trip {TripId} at stop {RouteStopId}",
                request.TripId, request.RouteStopId);
            throw new RpcException(new Status(StatusCode.Internal, "Error querying boarding data"));
        }
    }
}