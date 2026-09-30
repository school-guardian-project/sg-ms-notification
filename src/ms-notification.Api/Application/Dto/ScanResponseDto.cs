namespace ms_notification.Api.Application.Dto;

public class ScanResponseDto
{
    public Guid BoardingId { get; set; }
    public Guid AlertId { get; set; }
    public int ParentsNotified { get; set; }
}
