using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Tests.Fakes;

internal sealed class FakeFamilyDirectory : IFamilyDirectory
{
    public IReadOnlyList<Guid> ParentProfileIds { get; set; } = Array.Empty<Guid>();

    /// <summary>When set, the lookup fails the way a downed ms-user-management would.</summary>
    public Exception? ThrowOnRead { get; set; }

    public Task<IReadOnlyList<Guid>> GetParentProfileIdsAsync(
        Guid studentProfileId, CancellationToken ct = default)
        => ThrowOnRead is null
            ? Task.FromResult(ParentProfileIds)
            : Task.FromException<IReadOnlyList<Guid>>(ThrowOnRead);
}
