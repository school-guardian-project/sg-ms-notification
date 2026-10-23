using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto;

/// <summary>
/// Alerta simplificada para listados (paginados) en el dashboard admin.
/// Combina Alert + datos resueltos (nombre estudiante, ruta, tipo).
/// </summary>
public class AlertListDto
{
    public Guid Id { get; set; }
    public Guid RouteExecutionId { get; set; }
    public Guid ProfileId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public BoardingType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
}
