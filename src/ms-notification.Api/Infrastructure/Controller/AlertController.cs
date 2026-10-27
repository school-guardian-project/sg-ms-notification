using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Security;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class AlertController : ControllerBase
{
    private readonly IGetAlertsUseCase _getAlertsService;
    private readonly IAcknowledgeAlertUseCase _acknowledgeAlertService;
    private readonly IGetAlertByIdUseCase _getAlertByIdService;

    public AlertController(
        IGetAlertsUseCase getAlertsService,
        IAcknowledgeAlertUseCase acknowledgeAlertService,
        IGetAlertByIdUseCase getAlertByIdService)
    {
        _getAlertsService = getAlertsService;
        _acknowledgeAlertService = acknowledgeAlertService;
        _getAlertByIdService = getAlertByIdService;
    }

    [HttpGet("alerts")]
    [RequireRole(RequireRoleAttribute.AdminRoleId, RequireRoleAttribute.SuperAdminRoleId)]
    public async Task<ActionResult<AlertsResponse>> GetAlerts(
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sort = "timestamp,desc",
        [FromQuery] BoardingType? alertType = null,
        [FromQuery] bool? acknowledged = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var filters = new AlertFilters
        {
            Page = page,
            Size = size,
            Sort = sort,
            AlertType = alertType,
            Acknowledged = acknowledged,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Search = search
        };

        var result = await _getAlertsService.ExecuteAsync(filters, ct);
        return Ok(result);
    }

    [HttpPost("alerts/{alertId:guid}/acknowledge")]
    [RequireRole(RequireRoleAttribute.AdminRoleId, RequireRoleAttribute.SuperAdminRoleId)]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId, CancellationToken ct)
    {
        try
        {
            await _acknowledgeAlertService.ExecuteAsync(alertId, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("alerts/{alertId:guid}")]
    [RequireRole(RequireRoleAttribute.AdminRoleId, RequireRoleAttribute.SuperAdminRoleId)]
    public async Task<ActionResult<AlertDetailResponse>> GetAlertById(Guid alertId, CancellationToken ct)
    {
        var alert = await _getAlertByIdService.ExecuteAsync(alertId, ct);
        if (alert is null)
        {
            return NotFound();
        }

        return Ok(alert);
    }
}