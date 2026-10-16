namespace ms_notification.Api.Application.Dto;

public class IncidentResponseDto
{
    public Guid AlertId { get; set; }
    public Guid RouteExecutionId { get; set; }

    /// <summary>Estudiantes que iban a bordo cuando se reporto el incidente.</summary>
    public int StudentsOnBoard { get; set; }

    /// <summary>A quien se le registro la alerta: colegio + acudientes (FR-8.2).</summary>
    public int RecipientsNotified { get; set; }
}
