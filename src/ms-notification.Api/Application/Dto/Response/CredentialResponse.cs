namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Response DTO for student credential (QR token).
/// </summary>
/// <param name="QrToken">QR token for the student.</param>
/// <param name="ExpiresAt">Token expiration date/time.</param>
public sealed record CredentialResponse(
    string QrToken,
    DateTimeOffset ExpiresAt);