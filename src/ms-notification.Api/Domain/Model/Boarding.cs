namespace ms_notification.Api.Domain.Model;

public class Boarding
{
    public Guid Id { get; set; }
    public Guid RouteExecutionId { get; set; }
    public Guid ProfileId { get; set; }
    public Guid RouteStopId { get; set; }
    public DateTime DateTime { get; set; }
    public string BoardingType { get; set; } = "ON_BOARD";
}
