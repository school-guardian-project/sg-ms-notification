using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Security;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class ScanController : ControllerBase
{
    private readonly IProcessScanUseCase _processScanService;

    public ScanController(IProcessScanUseCase processScanService)
    {
        _processScanService = processScanService;
    }

    [EnableRateLimiting("writes")]
    [HttpPost("scan")]
    [RequireRole(RequireRoleAttribute.DriverRoleId)]
    [RequireProfile]
    public async Task<IActionResult> Scan([FromBody] ScanRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await _processScanService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (RpcException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
