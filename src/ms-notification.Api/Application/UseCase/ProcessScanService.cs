using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Messaging.Kafka;

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

        await _eventPublisher.PublishAsync("student.scanned", new
        {
            BoardingId = boarding.Id,
            AlertId = alert.Id,
            StudentProfileId = request.StudentProfileId,
            RouteExecutionId = request.RouteExecutionId,
            BoardingType = request.BoardingType,
            Timestamp = DateTime.UtcNow
        });

        return new ScanResponseDto
        {
            BoardingId = boarding.Id,
            AlertId = alert.Id,
            ParentsNotified = 0
        };
    }
}
