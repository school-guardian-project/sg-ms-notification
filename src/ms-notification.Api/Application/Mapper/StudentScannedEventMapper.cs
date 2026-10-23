using ms_notification.Api.Application.Dto.RouteDirectory;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Event;

namespace ms_notification.Api.Application.Mapper;

public static class StudentScannedEventMapper
{
    public static StudentScannedEvent ToEvent(
        Guid boardingId, Guid alertId, Guid studentProfileId, Guid routeExecutionId,
        BoardingType boardingType, StudentStopOnRoute stop)
    {
        return new StudentScannedEvent
        {
            BoardingId = boardingId,
            AlertId = alertId,
            StudentProfileId = studentProfileId,
            RouteExecutionId = routeExecutionId,
            BoardingType = boardingType,
            Timestamp = DateTime.UtcNow,
            StopName = stop.StopName,
            StopAddress = stop.Address,
            Latitude = stop.Latitude,
            Longitude = stop.Longitude
        };
    }
}
