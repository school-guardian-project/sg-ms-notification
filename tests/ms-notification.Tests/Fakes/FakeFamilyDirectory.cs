using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Tests.Fakes;

internal sealed class FakeFamilyDirectory : IFamilyDirectory
{
    public IReadOnlyList<Guid> ParentProfileIds { get; set; } = Array.Empty<Guid>();

    /// <summary>Answers that only apply to one student; overrides ParentProfileIds.</summary>
    public Dictionary<Guid, IReadOnlyList<Guid>> ByStudent { get; } = new();

    /// <summary>Students the directory was asked about, so a test can assert the fan-out.</summary>
    public List<Guid> Queried { get; } = new();

    /// <summary>When set, every lookup fails the way a downed ms-user-management would.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>Fails only for one student, leaving the others resolvable.</summary>
    public Dictionary<Guid, Exception> ThrowByStudent { get; } = new();

    public Task<IReadOnlyList<Guid>> GetParentProfileIdsAsync(
        Guid studentProfileId, CancellationToken ct = default)
    {
        Queried.Add(studentProfileId);

        if (ThrowOnRead is not null)
        {
            return Task.FromException<IReadOnlyList<Guid>>(ThrowOnRead);
        }

        if (ThrowByStudent.TryGetValue(studentProfileId, out var error))
        {
            return Task.FromException<IReadOnlyList<Guid>>(error);
        }

        return Task.FromResult(
            ByStudent.TryGetValue(studentProfileId, out var ids) ? ids : ParentProfileIds);
    }
}
