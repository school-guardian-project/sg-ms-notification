using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Security;

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
    public async Task<ActionResult<IncidentResponse>> ReportIncident(
        [FromBody] ReportIncidentRequest request, CancellationToken ct)
    {
        try
        {
            var response = await _reportIncidentService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), request, ct);
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