using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ProcessScanService _processScanService;
    private readonly NotificationContext _context;

    public NotificationController(ProcessScanService processScanService, NotificationContext context)
    {
        _processScanService = processScanService;
        _context = context;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequestDto request)
    {
        var result = await _processScanService.ExecuteAsync(request);
        return Ok(result);
    }

    [HttpGet("guardian/{profileId}")]
    public IActionResult GetGuardianNotifications(Guid profileId)
    {
        var notifications = _context.AlertRecipients
            .Where(ar => ar.ProfileId == profileId)
            .OrderByDescending(ar => ar.DateTimeRead)
            .Select(ar => new
            {
                ar.Id,
                ar.AlertId,
                ar.DateTimeRead,
                Alert = new
                {
                    ar.Alert.Id,
                    ar.Alert.Description,
                    ar.Alert.DateTime,
                    ar.Alert.BoardingType
                }
            })
            .ToList();

        return Ok(notifications);
    }
}
