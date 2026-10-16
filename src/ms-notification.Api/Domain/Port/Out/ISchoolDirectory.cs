namespace ms_notification.Api.Domain.Port.Out;

/// <summary>
/// Directorio del colegio: resuelve que administradores deben enterarse de un
/// incidente reportado por un perfil (FR-8.2). El adapter (Infrastructure) lee
/// Iam.Profile, School.SchoolCampus y School.SchoolAdmin; asi Application no
/// conoce el esquema ajeno.
/// </summary>
public interface ISchoolDirectory
{
    /// <summary>
    /// Perfiles activos de los administradores del colegio al que pertenece el
    /// perfil indicado (el del conductor que reporta).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetSchoolAdminProfileIdsAsync(
        Guid profileId, CancellationToken ct = default);
}
