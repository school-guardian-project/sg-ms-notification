using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.UseCase;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ProcessScanService _processScanService;

    public NotificationController(ProcessScanService processScanService)
    {
        _processScanService = processScanService;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequestDto request)
    {
        var result = await _processScanService.ExecuteAsync(request);
        return Ok(result);
    }
}
