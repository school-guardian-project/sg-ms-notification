using ms_notification.Api.Application.Dto.Request;

namespace ms_notification.Api.Domain.Port.In;

/// <summary>
/// Caso de uso: registrar (o mover) el token push de un dispositivo. El perfil
/// sale del token de sesion; el cuerpo solo trae el token del dispositivo.
/// </summary>
public interface IRegisterDeviceUseCase
{
    Task ExecuteAsync(Guid profileId, RegisterDeviceRequest request, CancellationToken ct = default);
}