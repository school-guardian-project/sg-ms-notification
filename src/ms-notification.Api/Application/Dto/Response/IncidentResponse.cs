namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Response DTO for reporting an incident.
/// </summary>
/// <param name="AlertId">Created alert identifier.</param>
/// <param name="RouteExecutionId">Route execution identifier.</param>
/// <param name="StudentsOnBoard">Students on board when incident was reported.</param>
/// <param name="RecipientsNotified">Number of recipients notified (school + guardians FR-8.2).</param>
public sealed record IncidentResponse(
    Guid AlertId,
    Guid RouteExecutionId,
    int StudentsOnBoard,
    int RecipientsNotified);