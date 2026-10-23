using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetAlertByIdUseCase
{
    /// <summary>Devuelve la alerta con ese id, o null si no existe.</summary>
    Task<AlertListDto?> ExecuteAsync(Guid alertId, CancellationToken ct = default);
}
