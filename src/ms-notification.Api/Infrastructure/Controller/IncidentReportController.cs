using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Auth;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class IncidentReportController : ControllerBase
{
    private readonly IReportIncidentUseCase _reportIncidentService;

    public IncidentReportController(IReportIncidentUseCase reportIncidentService)
    {
        _reportIncidentService = reportIncidentService;
    }

    [EnableRateLimiting("writes")]
    [HttpPost("incidents")]
    [RequireRole(RequireRoleAttribute.DriverRoleId)]
    [RequireProfile]
    public async Task<IActionResult> ReportIncident(
        [FromBody] ReportIncidentRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await _reportIncidentService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), request, ct));
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
