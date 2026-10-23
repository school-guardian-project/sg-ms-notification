namespace ms_notification.Api.Domain.Port.Out;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T @event);
}
