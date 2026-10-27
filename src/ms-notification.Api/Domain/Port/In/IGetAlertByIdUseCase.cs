using ms_notification.Api.Application.Dto.Response;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetAlertByIdUseCase
{
    /// <summary>
    /// Devuelve la alerta con ese id, con la misma forma que una fila del listado,
    /// o null si no existe.
    /// </summary>
    Task<AlertListResponse?> ExecuteAsync(Guid alertId, CancellationToken ct = default);
}