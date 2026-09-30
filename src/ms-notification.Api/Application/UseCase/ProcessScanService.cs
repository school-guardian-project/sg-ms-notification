using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Messaging.Kafka;
using ms_notification.Api.Application.Mapper;

namespace ms_notification.Api.Application.UseCase;

public class ProcessScanService
{
    private readonly NotificationContext _context;
    private readonly IEventPublisher _eventPublisher;

    public ProcessScanService(NotificationContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<ScanResponseDto> ExecuteAsync(ScanRequestDto request)
    {
        var boarding = new Boarding
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = request.RouteExecutionId,
            ProfileId = request.StudentProfileId,
            RouteStopId = request.RouteStopId,
            DateTime = DateTime.UtcNow,
            BoardingType = request.BoardingType
        };

        _context.Boardings.Add(boarding);

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = request.RouteExecutionId,
            AlertTypeId = 1,
            ProfileId = request.StudentProfileId,
            DateTime = DateTime.UtcNow,
            Description = request.BoardingType == "ON_BOARD" ? "Estudiante abordó" : "Estudiante descendió"
        };

        _context.Alerts.Add(alert);

        await _context.SaveChangesAsync();

        var eventData = StudentScannedEventMapper.ToEvent(
            boarding.Id,
            alert.Id,
            request.StudentProfileId,
            request.RouteExecutionId,
            request.BoardingType
        );

        await _eventPublisher.PublishAsync("student.scanned", eventData);

        return new ScanResponseDto
        {
            BoardingId = boarding.Id,
            AlertId = alert.Id,
            ParentsNotified = 0
        };
    }
}
