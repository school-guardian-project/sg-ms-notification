namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Paginated response of alerts.
/// </summary>
/// <param name="Content">Array of alert list items.</param>
/// <param name="TotalElements">Total number of elements.</param>
/// <param name="TotalPages">Total number of pages.</param>
/// <param name="Size">Page size.</param>
/// <param name="Number">Page number (0-based).</param>
public sealed record AlertsResponse(
    AlertListResponse[] Content,
    int TotalElements,
    int TotalPages,
    int Size,
    int Number);