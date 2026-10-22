namespace ms_notification.Api.Domain.Port.In;

public interface IResolveIncidentUseCase
{
    Task ExecuteAsync(Guid incidentId, CancellationToken ct = default);
}