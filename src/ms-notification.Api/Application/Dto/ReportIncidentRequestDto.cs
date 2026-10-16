namespace ms_notification.Api.Application.Dto;

/// <summary>
/// Cuerpo del reporte de incidente (FR-8.1). El tipo sale del catalogo
/// Notification.AlertType; la descripcion es opcional.
/// </summary>
public class ReportIncidentRequestDto
{
    public byte AlertTypeId { get; set; }
    public string? Description { get; set; }
}
