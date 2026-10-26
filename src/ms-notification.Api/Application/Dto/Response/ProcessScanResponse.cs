namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Response DTO for processing a student scan.
/// </summary>
/// <param name="BoardingId">Created boarding record identifier.</param>
/// <param name="AlertId">Created alert identifier.</param>
/// <param name="ParentsNotified">Number of parents notified.</param>
/// <param name="AlreadyProcessed">True when the scan was already processed (idempotent).</param>
public sealed record ProcessScanResponse(
    Guid BoardingId,
    Guid AlertId,
    int ParentsNotified,
    bool AlreadyProcessed = false);