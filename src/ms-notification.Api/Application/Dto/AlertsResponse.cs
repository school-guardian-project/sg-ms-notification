namespace ms_notification.Api.Application.Dto;

/// <summary>
/// Respuesta paginada de alertas.
/// </summary>
public class AlertsResponse
{
    public AlertListDto[] Content { get; set; } = Array.Empty<AlertListDto>();
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public int Size { get; set; }
    public int Number { get; set; }
}
