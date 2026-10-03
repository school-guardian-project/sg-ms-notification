namespace ms_notification.Api.Application.Dto;

public class ScanRequestDto
{
    public Guid RouteId { get; set; }
    public Guid RouteExecutionId { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid RouteStopId { get; set; }
    public string BoardingType { get; set; } = "ON_BOARD";
}
