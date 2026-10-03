using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Messaging.Kafka;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Persistence.Entity;

namespace ms_notification.Api.Application.UseCase;

public class ProcessScanService
{
    private const int MaxDescriptionLength = 100;

    private readonly NotificationContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProcessScanService> _logger;

    public ProcessScanService(
        NotificationContext context,
        IEventPublisher eventPublisher,
        IHttpClientFactory httpClientFactory,
        ILogger<ProcessScanService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ScanResponseDto> ExecuteAsync(ScanRequestDto request)
    {
        var stopName = (string?)null;
        var routeName = (string?)null;

        if (request.RouteId != Guid.Empty)
        {
            var (assigned, assignedStopName) = await GetAssignedStopAsync(request.RouteId, request.StudentProfileId);
            if (!assigned)
            {
                throw new InvalidOperationException("Student not assigned to this route");
            }

            stopName = assignedStopName;
            routeName = await GetRouteNameAsync(request.RouteId);
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

        var studentName = await GetStudentNameAsync(request.StudentProfileId);
        var description = BuildDescription(request.BoardingType, studentName, routeName, stopName);

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
            Description = description
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
            ParentsNotified = await CountParentsAsync(request.StudentProfileId)
        };
    }

    private static string BuildDescription(string boardingType, string? studentName, string? routeName, string? stopName)
    {
        var action = boardingType == "OFF_BOARD" ? "bajó del bus" : "subió al bus";
        var who = string.IsNullOrWhiteSpace(studentName) ? "Estudiante" : studentName;

        var text = $"{who} {action}";

        if (!string.IsNullOrWhiteSpace(stopName))
        {
            text += $" en la parada {stopName}";
        }

        if (!string.IsNullOrWhiteSpace(routeName))
        {
            text += $" - {routeName}";
        }

        return text.Length > MaxDescriptionLength ? text[..MaxDescriptionLength] : text;
    }

    private async Task<(bool Assigned, string? StopName)> GetAssignedStopAsync(Guid routeId, Guid studentProfileId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ms-route");
            var response = await client.GetAsync($"/api/routes/{routeId}/students/{studentProfileId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, null);
            }

            if (!response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var stopName = document.RootElement.TryGetProperty("stopName", out var value)
                ? value.GetString()
                : null;

            return (true, stopName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify the assignment of student {StudentProfileId} on route {RouteId}", studentProfileId, routeId);
            return (true, null);
        }
    }

    private async Task<string?> GetRouteNameAsync(Guid routeId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ms-route");
            var response = await client.GetAsync($"/api/routes/{routeId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("name", out var value)
                ? value.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load the name of route {RouteId}", routeId);
            return null;
        }
    }

    private async Task<string?> GetStudentNameAsync(Guid studentProfileId)
    {
        try
        {
            var profile = await _context.Set<ProfileRefEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == studentProfileId);

            if (profile is null)
            {
                return null;
            }

            var person = await _context.Set<PersonRefEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == profile.PersonId);

            if (person is null)
            {
                return null;
            }

            return $"{person.Name} {person.LastName}".Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load the name of student {StudentProfileId}", studentProfileId);
            return null;
        }
    }

    private async Task<int> CountParentsAsync(Guid studentProfileId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ms-user-management");
            var response = await client.GetAsync($"/api/families/student/{studentProfileId}/members");

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not count the family members of student {StudentProfileId}", studentProfileId);
            return 0;
        }
    }
}
