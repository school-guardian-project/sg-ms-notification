using Grpc.Core;
using Grpc.Net.Client;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Api.Infrastructure.Grpc;

public sealed class RouteGrpcClient : IRouteDirectory, IDisposable
{
    // Holgura amplia: la primera llamada tras un arranque en frio compila el modelo
    // de EF en el servicio destino y puede tardar varios segundos.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly GrpcChannel _channel;
    private readonly RouteService.RouteServiceClient _client;

    public RouteGrpcClient(IConfiguration configuration)
    {
        _channel = GrpcChannel.ForAddress(
            configuration["Grpc:Route:Address"]
            ?? throw new InvalidOperationException("Missing Grpc__Route__Address (set it in .env)"));
        _client = new RouteService.RouteServiceClient(_channel);
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

            return new StudentRouteInfo(Guid.Parse(response.Id), response.Name);
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
                response.StopName)
            : null;
    }

    public async Task<CurrentTrip?> GetCurrentTripAsync(Guid driverId, CancellationToken ct = default)
    {
        var response = await _client.GetCurrentTripAsync(
            new GetCurrentTripRequest { DriverId = driverId.ToString() },
            deadline: DateTime.UtcNow.Add(Timeout),
            cancellationToken: ct);

        return response.Found
            ? new CurrentTrip(Guid.Parse(response.Id), Guid.Parse(response.BusId), response.Status)
            : null;
    }

    public void Dispose() => _channel.Dispose();
}