using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.UseCase;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ProcessScanService _processScanService;
    private readonly GetGuardianNotificationsService _guardianNotificationsService;

    public NotificationController(
        ProcessScanService processScanService,
        GetGuardianNotificationsService guardianNotificationsService)
    {
        _processScanService = processScanService;
        _guardianNotificationsService = guardianNotificationsService;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequestDto request)
    {
        var result = await _processScanService.ExecuteAsync(request);
        return Ok(result);
    }

    [HttpGet("guardian/{profileId}")]
    public async Task<IActionResult> GetGuardianNotifications(Guid profileId, CancellationToken ct)
    {
        var notifications = await _guardianNotificationsService.ExecuteAsync(profileId, ct);
        return Ok(notifications);
    }
}
