namespace ms_notification.Api.Application.Dto;

public class RegisterDeviceRequestDto
{
    public Guid ProfileId { get; set; }
    public string ExpoToken { get; set; } = string.Empty;
}
