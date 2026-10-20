using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Tests.Fakes;

/// <summary>
/// Stand-in for the school management service: returns the admins the test sets,
/// and records which profile was asked about.
/// </summary>
internal sealed class FakeSchoolDirectory : ISchoolDirectory
{
    public IReadOnlyList<Guid> SchoolAdminProfileIds { get; set; } = Array.Empty<Guid>();

    public List<Guid> Queried { get; } = new();

    /// <summary>When set, the lookup fails the way a downed ms-school-management would.</summary>
    public Exception? ThrowOnRead { get; set; }

    public Task<IReadOnlyList<Guid>> GetSchoolAdminProfileIdsAsync(
        Guid profileId, CancellationToken ct = default)
    {
        Queried.Add(profileId);

        return ThrowOnRead is null
            ? Task.FromResult(SchoolAdminProfileIds)
            : Task.FromException<IReadOnlyList<Guid>>(ThrowOnRead);
    }
}
