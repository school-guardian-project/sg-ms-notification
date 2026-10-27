using ms_notification.Api.Application.Dto.Response;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetIncidentByIdUseCase
{
    /// <summary>
    /// Devuelve el incidente con ese id, o null si no existe o la alerta no es
    /// un incidente (mismo filtro que el listado).
    /// </summary>
    Task<IncidentListResponse?> ExecuteAsync(Guid incidentId, CancellationToken ct = default);
}