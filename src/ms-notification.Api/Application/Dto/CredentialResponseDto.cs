namespace ms_notification.Api.Application.Dto;

public class CredentialResponseDto
{
    public string QrToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}