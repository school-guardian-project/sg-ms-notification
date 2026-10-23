using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.Out;

/// <summary>
/// Directorio de estudiantes: resuelve los datos de la persona detras de un
/// perfil. El adapter (Infrastructure) lee Iam.Profile y UserManagement.Person;
/// asi Application no conoce el DbContext ni el esquema ajeno.
/// </summary>
public interface IStudentDirectory
{
    /// <summary>Nombre completo del estudiante, o null si no se puede resolver.</summary>
    Task<string?> GetStudentNameAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Información completa del estudiante.</summary>
    Task<StudentInfo?> GetStudentByProfileIdAsync(Guid studentProfileId, CancellationToken ct = default);
}
