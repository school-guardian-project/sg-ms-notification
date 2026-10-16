using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

/// <summary>
/// Caso de uso (FR-8.1, HU-INCIDENT-001): el conductor reporta un incidente o
/// emergencia durante su viaje. La identidad del conductor llega del token de
/// sesion, no del cuerpo de la peticion.
/// </summary>
public interface IReportIncidentUseCase
{
    Task<IncidentResponseDto> ExecuteAsync(
        Guid driverProfileId, ReportIncidentRequestDto request, CancellationToken ct = default);
}
