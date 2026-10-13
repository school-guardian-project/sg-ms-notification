using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

/// <summary>
/// Caso de uso: listar las notificaciones del acudiente autenticado. El perfil
/// sale del token de sesion, asi que la peticion no lo lleva.
/// </summary>
public interface IGetGuardianNotificationsUseCase
{
    Task<List<GuardianNotificationDto>> ExecuteAsync(Guid profileId, CancellationToken ct = default);
}
