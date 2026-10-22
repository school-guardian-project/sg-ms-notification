using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Persistence.Entity;

namespace ms_notification.Api.Infrastructure.Persistence.Directory;

/// <summary>
/// Adapter EF de IStudentDirectory: resuelve el nombre del estudiante cruzando
/// Iam.Profile con UserManagement.Person (mapeadas como entidades sin clave).
/// </summary>
public class StudentDirectory : IStudentDirectory
{
    private readonly NotificationContext _context;

    public StudentDirectory(NotificationContext context)
    {
        _context = context;
    }

    public async Task<string?> GetStudentNameAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var info = await GetStudentByProfileIdAsync(studentProfileId, ct);
        return info?.FullName;
    }

    public async Task<IStudentDirectory.StudentInfo?> GetStudentByProfileIdAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var profile = await _context.Set<ProfileRefEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == studentProfileId, ct);

        if (profile is null)
        {
            return null;
        }

        var person = await _context.Set<PersonRefEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == profile.PersonId, ct);

        if (person is null)
        {
            return null;
        }

        return new IStudentDirectory.StudentInfo(
            ProfileId: studentProfileId,
            FullName: $"{person.Name} {person.LastName}".Trim(),
            Email: person.Email,
            Phone: person.Phone.ToString());
    }
}
