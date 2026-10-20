namespace ms_notification.Api.Domain.Port.Out;

public interface IFamilyDirectory
{
    Task<IReadOnlyList<Guid>> GetParentProfileIdsAsync(
        Guid studentProfileId, CancellationToken ct = default);
}