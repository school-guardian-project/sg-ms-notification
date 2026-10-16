using ms_notification.Api.Infrastructure.Messaging.Kafka;

namespace ms_notification.Tests.Fakes;

internal sealed record PublishedEvent(string Topic, object Payload);

/// <summary>Captures what would have gone to Kafka instead of requiring a broker.</summary>
internal sealed class RecordingEventPublisher : IEventPublisher
{
    public List<PublishedEvent> Published { get; } = new();

    public Task PublishAsync<T>(string topic, T @event)
    {
        Published.Add(new PublishedEvent(topic, @event!));
        return Task.CompletedTask;
    }
}
