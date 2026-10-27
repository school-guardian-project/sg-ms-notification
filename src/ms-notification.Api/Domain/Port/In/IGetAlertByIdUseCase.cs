using ms_notification.Api.Application.Dto.Response;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetAlertByIdUseCase
{
    /// <summary>Devuelve la alerta con ese id, o null si no existe.</summary>
    Task<AlertDetailResponse?> ExecuteAsync(Guid alertId, CancellationToken ct = default);
}