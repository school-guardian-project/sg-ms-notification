namespace ms_notification.Api.Domain.Model;

/// <summary>
/// Información básica de un estudiante para directorios.
/// </summary>
public sealed record StudentInfo(
    Guid ProfileId,
    string FullName,
    string? IdentificationNumber,
    string? Email,
    string? PhoneNumber);