namespace ms_notification.Api.Domain.Model;

public class AlertType
{
    public byte Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte UrgencyLevel { get; set; }
}
