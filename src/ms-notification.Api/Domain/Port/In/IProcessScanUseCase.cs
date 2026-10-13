using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

/// <summary>
/// Caso de uso: registrar el escaneo de una credencial. La identidad del
/// conductor llega del token de sesion, no del cuerpo de la peticion.
/// </summary>
public interface IProcessScanUseCase
{
    Task<ScanResponseDto> ExecuteAsync(
        Guid driverProfileId, ScanRequestDto request, CancellationToken ct = default);
}
