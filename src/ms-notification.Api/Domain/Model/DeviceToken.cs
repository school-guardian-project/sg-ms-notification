namespace ms_notification.Api.Domain.Model;

public class DeviceToken
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string ExpoToken { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
