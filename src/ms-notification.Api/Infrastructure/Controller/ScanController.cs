using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Auth;

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
    public async Task<ActionResult<ProcessScanResponse>> Scan([FromBody] ProcessScanRequest request, CancellationToken ct)
    {
        try
        {
            var response = await _processScanService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), request, ct);
            return Ok(response);
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