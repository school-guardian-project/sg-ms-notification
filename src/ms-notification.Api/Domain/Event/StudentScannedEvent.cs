using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Domain.Event;

public class StudentScannedEvent
{
    public Guid BoardingId { get; set; }
    public Guid AlertId { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid RouteExecutionId { get; set; }
    public BoardingType BoardingType { get; set; }
    public DateTime Timestamp { get; set; }

    // Ubicacion de la parada asignada (FR-5.2, FR-5.3). El texto de la alerta
    // ya lleva la direccion; estos campos permiten que el canal de envio arme
    // su propio mensaje y ubique la parada sin volver a consultar ms-route.
    public string StopName { get; set; } = string.Empty;
    public string StopAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
