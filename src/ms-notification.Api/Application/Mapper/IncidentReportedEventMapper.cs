using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

public static class IncidentReportedEventMapper
{
    public static IncidentReportedEvent ToEvent(
        Alert alert, IReadOnlyCollection<Guid> studentsOnBoard, IReadOnlyCollection<Guid> recipients)
    {
        return new IncidentReportedEvent
        {
            AlertId = alert.Id,
            RouteExecutionId = alert.RouteExecutionId,
            ReportedByProfileId = alert.ProfileId,
            AlertTypeId = alert.AlertTypeId,
            Description = alert.Description,
            StudentsOnBoard = studentsOnBoard.ToList(),
            Recipients = recipients.ToList(),
            Timestamp = DateTime.UtcNow
        };
    }
}
