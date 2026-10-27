using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto.Response;

/// <summary>
/// Simplified alert for listings (paginated) in admin dashboard.
/// Combines Alert + resolved data (student name, route, type).
/// </summary>
/// <param name="Id">Alert identifier.</param>
/// <param name="RouteExecutionId">Route execution identifier.</param>
/// <param name="ProfileId">Profile identifier.</param>
/// <param name="StudentName">Student name.</param>
/// <param name="RouteName">Route name.</param>
/// <param name="AlertType">Alert type (boarding type).</param>
/// <param name="Message">Alert message.</param>
/// <param name="Timestamp">Alert timestamp.</param>
/// <param name="Acknowledged">Whether the alert has been acknowledged.</param>
/// <param name="AcknowledgedAt">Acknowledgment timestamp.</param>
/// <param name="AcknowledgedBy">Who acknowledged the alert.</param>
/// <param name="AlertTypeId">Catalog id (Notification.AlertType).</param>
/// <param name="AlertTypeName">Catalog name of the alert type.</param>
public sealed record AlertListResponse(
    Guid Id,
    Guid RouteExecutionId,
    Guid ProfileId,
    string StudentName,
    string RouteName,
    BoardingType AlertType,
    string Message,
    DateTime Timestamp,
    bool Acknowledged,
    DateTime? AcknowledgedAt,
    string? AcknowledgedBy,
    byte AlertTypeId,
    string AlertTypeName);
