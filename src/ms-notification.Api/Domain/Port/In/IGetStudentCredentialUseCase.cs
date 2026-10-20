using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

/// <summary>
/// Caso de uso: emitir la credencial QR firmada del estudiante autenticado.
/// El perfil sale del token de sesion, asi que la peticion no lo lleva.
/// </summary>
public interface IGetStudentCredentialUseCase
{
    Task<CredentialResponseDto> ExecuteAsync(Guid studentProfileId, CancellationToken ct = default);
}
