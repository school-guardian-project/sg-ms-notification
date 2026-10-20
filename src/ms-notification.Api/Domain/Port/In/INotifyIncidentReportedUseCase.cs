using ms_notification.Api.Domain.Event;

namespace ms_notification.Api.Domain.Port.In;

/// <summary>
/// Puerto de entrada del canal de entrega del incidente (FR-8.2): toma el evento
/// que publica el reporte y empuja la alerta a los dispositivos de los
/// destinatarios. Lo invoca el consumidor de Kafka, no un controlador.
/// </summary>
public interface INotifyIncidentReportedUseCase
{
    Task NotifyAsync(IncidentReportedEvent incident, CancellationToken ct = default);
}
