using Grpc.Core;
using Grpc.Net.Client;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Api.Infrastructure.Grpc;

public sealed class FamilyGrpcClient : IFamilyDirectory, IDisposable
{
    // Holgura amplia: la primera llamada tras un arranque en frio compila el modelo
    // de EF en el servicio destino y puede tardar varios segundos.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly GrpcChannel _channel;
    private readonly FamilyService.FamilyServiceClient _client;

    public FamilyGrpcClient(IConfiguration configuration)
    {
        _channel = GrpcChannel.ForAddress(
            configuration["Grpc:Family:Address"]
            ?? throw new InvalidOperationException("Missing Grpc__Family__Address (set it in .env)"));
        _client = new FamilyService.FamilyServiceClient(_channel);
    }

    public async Task<IReadOnlyList<Guid>> GetParentProfileIdsAsync(
        Guid studentProfileId, CancellationToken ct = default)
    {
        var response = await _client.GetFamilyMembersByStudentAsync(
            new GetFamilyMembersByStudentRequest { StudentProfileId = studentProfileId.ToString() },
            deadline: DateTime.UtcNow.Add(Timeout),
            cancellationToken: ct);

        return response.ParentProfileIds.Select(Guid.Parse).ToList();
    }

    public void Dispose() => _channel.Dispose();
}