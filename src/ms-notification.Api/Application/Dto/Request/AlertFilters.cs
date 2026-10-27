using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto.Request;

/// <summary>
/// Filtros para listado de alertas.
/// </summary>
public sealed class AlertFilters
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string Sort { get; init; } = "timestamp,desc";
    public BoardingType? AlertType { get; init; }
    public byte? AlertTypeId { get; init; }
    public Guid? SchoolId { get; init; }
    public Guid? AdminProfileId { get; init; }
    public bool? Acknowledged { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
}