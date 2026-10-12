using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.UseCase;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ProcessScanService _processScanService;
    private readonly GetGuardianNotificationsService _guardianNotificationsService;
    private readonly RegisterDeviceService _registerDeviceService;

    public NotificationController(
        ProcessScanService processScanService,
        GetGuardianNotificationsService guardianNotificationsService,
        RegisterDeviceService registerDeviceService)
    {
        _processScanService = processScanService;
        _guardianNotificationsService = guardianNotificationsService;
        _registerDeviceService = registerDeviceService;
    }

    [EnableRateLimiting("writes")]
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

    [EnableRateLimiting("writes")]
    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequestDto request, CancellationToken ct)
    {
        await _registerDeviceService.ExecuteAsync(request, ct);
        return Ok();
    }
}
