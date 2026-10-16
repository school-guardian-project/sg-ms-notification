namespace ms_notification.Api.Domain.Event;

/// <summary>
/// Evento publicado al registrar un incidente (FR-8.1, HU-INCIDENT-001). Lleva
/// el alertId y a quienes corresponde avisar, para que el canal de entrega no
/// tenga que reconstruir la lista de destinatarios ni los estudiantes a bordo.
/// </summary>
public class IncidentReportedEvent
{
    public Guid AlertId { get; set; }
    public Guid RouteExecutionId { get; set; }
    public Guid ReportedByProfileId { get; set; }
    public byte AlertTypeId { get; set; }
    public string? Description { get; set; }

    /// <summary>Estudiantes que iban a bordo cuando se reporto el incidente.</summary>
    public List<Guid> StudentsOnBoard { get; set; } = new();

    /// <summary>Perfiles a los que se les registro la alerta (colegio + acudientes).</summary>
    public List<Guid> Recipients { get; set; } = new();

    public DateTime Timestamp { get; set; }
}
