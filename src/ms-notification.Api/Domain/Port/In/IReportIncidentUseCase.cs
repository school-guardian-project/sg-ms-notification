using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Application.Dto.Response;

namespace ms_notification.Api.Domain.Port.In;

/// <summary>
/// Caso de uso (FR-8.1, HU-INCIDENT-001): el conductor reporta un incidente o
/// emergencia durante su viaje. La identidad del conductor llega del token de
/// sesion, no del cuerpo de la peticion.
/// </summary>
public interface IReportIncidentUseCase
{
    Task<IncidentResponse> ExecuteAsync(
        Guid driverProfileId, ReportIncidentRequest request, CancellationToken ct = default);
}