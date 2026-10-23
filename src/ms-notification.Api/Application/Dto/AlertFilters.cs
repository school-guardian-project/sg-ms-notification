using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto;

/// <summary>
/// Filtros para listado de alertas.
/// </summary>
public class AlertFilters
{
    public int Page { get; set; } = 0;
    public int Size { get; set; } = 10;
    public string Sort { get; set; } = "timestamp,desc";
    public BoardingType? AlertType { get; set; }
    public bool? Acknowledged { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }
}
