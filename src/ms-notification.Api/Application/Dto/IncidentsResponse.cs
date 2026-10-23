namespace ms_notification.Api.Application.Dto;

/// <summary>
/// Respuesta paginada de incidentes.
/// </summary>
public class IncidentsResponse
{
    public IncidentListDto[] Content { get; set; } = Array.Empty<IncidentListDto>();
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public int Size { get; set; }
    public int Number { get; set; }
}
