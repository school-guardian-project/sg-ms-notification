using System.ComponentModel.DataAnnotations;

namespace ms_notification.Api.Application.Dto.Request;

/// <summary>
/// Request DTO for registering a device token for push notifications.
/// </summary>
public sealed class RegisterDeviceRequest
{
    /// <summary>
    /// Expo push token for the device.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string ExpoToken { get; init; } = string.Empty;
}