using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Api.Infrastructure.Messaging.Observer;

/// <summary>
/// Observador central para eventos de escaneo de estudiantes.
/// Implementa el patrón Observer para notificar a múltiples suscriptores.
/// </summary>
public sealed class StudentScannedEventObserver
{
    private readonly IReadOnlyList<IStudentScannedSubscriber> _subscribers;

    public StudentScannedEventObserver(IEnumerable<IStudentScannedSubscriber> subscribers)
    {
        _subscribers = subscribers.ToList();
    }

    public async Task NotifyAsync(StudentScannedEvent @event, CancellationToken ct = default)
    {
        foreach (var subscriber in _subscribers)
        {
            try
            {
                await subscriber.HandleStudentScannedAsync(@event, ct);
            }
            catch (Exception ex)
            {
                // Registro de error pero no interrumpir el procesamiento
                // de otros suscriptores
                Console.WriteLine($"Error al notificar a {subscriber.GetType().Name}: {ex.Message}");
            }
        }
    }
}