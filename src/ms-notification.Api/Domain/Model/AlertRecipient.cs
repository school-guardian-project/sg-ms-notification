namespace ms_notification.Api.Domain.Model;

public class AlertRecipient
{
    public Guid Id { get; set; }
    public Guid AlertId { get; set; }
    public Guid ProfileId { get; set; }
    public DateTime DateTimeRead { get; set; }
}
