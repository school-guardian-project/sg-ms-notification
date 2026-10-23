namespace ms_notification.Api.Application.Dto;

public class AlertDetailDto
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public DateTime DateTime { get; set; }
    public byte AlertTypeId { get; set; }
}
