namespace ms_notification.Api.Domain.Event;

public class StudentScannedEvent
{
    public Guid BoardingId { get; set; }
    public Guid AlertId { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid RouteExecutionId { get; set; }
    public string BoardingType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
