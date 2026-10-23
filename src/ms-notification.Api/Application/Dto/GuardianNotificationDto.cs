using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto;

public class GuardianNotificationDto
{
    public Guid Id { get; set; }
    public Guid AlertId { get; set; }
    public DateTime DateTimeRead { get; set; }
    public BoardingType? BoardingType { get; set; }
    public AlertDetailDto Alert { get; set; } = new();
}
