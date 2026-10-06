namespace ms_notification.Api.Domain.Model;

public class Alert
{
    public Guid Id { get; set; }
    public Guid RouteExecutionId { get; set; }
    public byte AlertTypeId { get; set; }
    public Guid ProfileId { get; set; }
    public DateTime DateTime { get; set; }
    public string? Description { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}
