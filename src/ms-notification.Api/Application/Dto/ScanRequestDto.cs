using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto;

public class ScanRequestDto
{
    public string QrToken { get; set; } = string.Empty;
    public BoardingType BoardingType { get; set; }
}
