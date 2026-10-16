using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Persistence.Entity;

namespace ms_notification.Api.Infrastructure.Persistence.Directory;

/// <summary>
/// Adapter EF de ISchoolDirectory: llega del perfil del conductor a los
/// administradores de su colegio cruzando Iam.Profile -> School.SchoolCampus ->
/// School.SchoolAdmin (entidades sin clave). Mismo patron que StudentDirectory.
/// </summary>
public class SchoolDirectory : ISchoolDirectory
{
    private const string ActiveStatus = "Active";

    private readonly NotificationContext _context;

    public SchoolDirectory(NotificationContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetSchoolAdminProfileIdsAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var admins =
            from profile in _context.Set<ProfileRefEntity>().AsNoTracking()
            join campus in _context.Set<SchoolCampusRefEntity>().AsNoTracking()
                on profile.CampuseId equals campus.Id
            join admin in _context.Set<SchoolAdminRefEntity>().AsNoTracking()
                on campus.SchoolId equals admin.SchoolId
            where profile.Id == profileId && admin.Status == ActiveStatus
            select admin.ProfileId;

        return await admins.Distinct().ToListAsync(ct);
    }
}
