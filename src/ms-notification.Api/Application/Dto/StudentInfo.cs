namespace ms_notification.Api.Application.Dto;

/// <summary>
/// Información básica del estudiante.
/// </summary>
public sealed record StudentInfo(
    Guid ProfileId,
    string FullName,
    string? Email,
    string? Phone);
