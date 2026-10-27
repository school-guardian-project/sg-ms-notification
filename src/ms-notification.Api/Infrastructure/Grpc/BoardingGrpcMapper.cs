using Grpc.Core;

namespace ms_notification.Api.Infrastructure.Grpc;

/// <summary>
/// Conversiones puras entre los contratos protobuf (boarding.proto) y los
/// valores de dominio. No tiene lógica de negocio ni acceso a datos:
/// BoardingGrpcService se encarga de invocar la lógica y traducir excepciones.
/// </summary>
public static class BoardingGrpcMapper
{
    /// <summary>
    /// Parsea un UUID desde string. Lanza RpcException con InvalidArgument si falla.
    /// </summary>
    public static Guid ParseId(string value, string field)
    {
        if (!Guid.TryParse(value, out var id))
            throw new RpcException(new Status(
                StatusCode.InvalidArgument, $"Invalid {field}, use UUID: {value}"));
        return id;
    }
}