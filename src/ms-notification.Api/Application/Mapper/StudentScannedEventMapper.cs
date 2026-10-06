using ms_notification.Api.Domain.Event;

namespace ms_notification.Api.Application.Mapper;

public static class StudentScannedEventMapper
{
    public static StudentScannedEvent ToEvent(Guid boardingId, Guid alertId, Guid studentProfileId, Guid routeExecutionId, string boardingType)
    {
        return new StudentScannedEvent
        {
            BoardingId = boardingId,
            AlertId = alertId,
            StudentProfileId = studentProfileId,
            RouteExecutionId = routeExecutionId,
            BoardingType = boardingType,
            Timestamp = DateTime.UtcNow
        };
    }
}
