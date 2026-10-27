namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Response DTO for alert detail.
/// </summary>
/// <param name="Id">Alert identifier.</param>
/// <param name="Description">Alert description.</param>
/// <param name="DateTime">Alert date/time.</param>
/// <param name="AlertTypeId">Alert type identifier.</param>
public sealed record AlertDetailResponse(
    Guid Id,
    string? Description,
    DateTime DateTime,
    byte AlertTypeId);