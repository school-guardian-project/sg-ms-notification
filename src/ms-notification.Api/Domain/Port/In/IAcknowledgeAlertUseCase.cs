namespace ms_notification.Api.Domain.Port.In;

public interface IAcknowledgeAlertUseCase
{
    Task ExecuteAsync(Guid alertId, CancellationToken ct = default);
}