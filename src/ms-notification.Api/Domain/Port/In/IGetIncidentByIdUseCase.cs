using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetIncidentByIdUseCase
{
    /// <summary>
    /// Devuelve el incidente con ese id, o null si no existe o la alerta no es
    /// un incidente (mismo filtro que el listado).
    /// </summary>
    Task<IncidentListDto?> ExecuteAsync(Guid incidentId, CancellationToken ct = default);
}
