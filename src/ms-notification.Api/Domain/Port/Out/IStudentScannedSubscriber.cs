using ms_notification.Api.Domain.Event;

namespace ms_notification.Api.Domain.Port.Out;

public interface IStudentScannedSubscriber
{
    Task HandleStudentScannedAsync(StudentScannedEvent @event, CancellationToken ct = default);
}