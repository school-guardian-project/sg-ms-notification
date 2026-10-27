using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Auth;
using ms_notification.Api.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class AlertController : ControllerBase
{
    private readonly IGetAlertsUseCase _getAlertsService;
    private readonly IAcknowledgeAlertUseCase _acknowledgeAlertService;
    private readonly IGetAlertByIdUseCase _getAlertByIdService;
    private readonly NotificationContext _context;

    public AlertController(
        IGetAlertsUseCase getAlertsService,
        IAcknowledgeAlertUseCase acknowledgeAlertService,
        IGetAlertByIdUseCase getAlertByIdService,
        NotificationContext context)
    {
        _getAlertsService = getAlertsService;
        _acknowledgeAlertService = acknowledgeAlertService;
        _getAlertByIdService = getAlertByIdService;
        _context = context;
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
        [FromQuery] byte? alertTypeId = null,
        [FromQuery] Guid? schoolId = null,
        CancellationToken ct = default)
    {
        if (page < 0 || size < 1 || size > 100)
        {
            return BadRequest(new { message = "page debe ser >= 0 y size debe estar entre 1 y 100." });
        }

        var isAdmin = User.FindFirstValue(SgClaimTypes.RoleId) == RequireRoleAttribute.AdminRoleId;
        var profileId = NotificationClaims.CurrentProfileId(User);
        if (isAdmin && profileId == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var filters = new AlertFilters
        {
            Page = page,
            Size = size,
            Sort = sort,
            AlertType = alertType,
            Acknowledged = acknowledged,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Search = search,
            AlertTypeId = alertTypeId,
            SchoolId = schoolId,
            AdminProfileId = isAdmin ? profileId : null
        };

        var result = await _getAlertsService.ExecuteAsync(filters, ct);
        return Ok(result);
    }

    [HttpGet("alert-types")]
    [RequireRole(RequireRoleAttribute.AdminRoleId, RequireRoleAttribute.SuperAdminRoleId)]
    public async Task<IActionResult> GetAlertTypes(CancellationToken ct)
    {
        var types = await _context.AlertTypes.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);
        return Ok(types);
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
    public async Task<ActionResult<AlertListResponse>> GetAlertById(Guid alertId, CancellationToken ct)
    {
        var alert = await _getAlertByIdService.ExecuteAsync(alertId, ct);
        if (alert is null)
        {
            return NotFound();
        }

        return Ok(alert);
    }
}