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
    private readonly IHttpClientFactory _httpClientFactory;

    public ProcessScanService(NotificationContext context, IEventPublisher eventPublisher, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ScanResponseDto> ExecuteAsync(ScanRequestDto request)
    {
        if (!await IsStudentAssignedToRouteAsync(request.StudentProfileId, request.RouteExecutionId))
        {
            throw new InvalidOperationException("Student not assigned to this route");
        }

        if (request.BoardingType == "OFF_BOARD")
        {
            var hasActiveBoarding = _context.Boardings.Any(b =>
                b.ProfileId == request.StudentProfileId &&
                b.RouteExecutionId == request.RouteExecutionId &&
                b.BoardingType == "ON_BOARD");

            if (!hasActiveBoarding)
            {
                throw new InvalidOperationException("No active boarding for the student");
            }
        }

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
            AlertTypeId = request.BoardingType == "ON_BOARD" ? (byte)1 : (byte)2,
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

    private async Task<bool> IsStudentAssignedToRouteAsync(Guid studentProfileId, Guid routeExecutionId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://ms-route:8080/api/v1/routes/{routeExecutionId}/students/{studentProfileId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return true;
        }
    }
}
