namespace ms_notification.Api.Application.Dto;

public class ScanResponseDto
{
    public Guid BoardingId { get; set; }
    public Guid AlertId { get; set; }
    public int ParentsNotified { get; set; }

    // true cuando el escaneo ya estaba registrado y se devuelve sin volver a
    // notificar. Es aditivo: los clientes actuales lo ignoran.
    public bool AlreadyProcessed { get; set; }
}
