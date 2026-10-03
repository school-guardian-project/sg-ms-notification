namespace ms_notification.Api.Application.Dto;

public class GuardianNotificationDto
{
    public Guid Id { get; set; }
    public Guid AlertId { get; set; }
    public DateTime DateTimeRead { get; set; }
    public string? BoardingType { get; set; }
    public AlertDetailDto Alert { get; set; } = new();
}

public class AlertDetailDto
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public DateTime DateTime { get; set; }
    public byte AlertTypeId { get; set; }
}
