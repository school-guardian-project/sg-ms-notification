using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Port.In;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private const string DriverRoleId = "3";
    private const string AdminRoleId = "1";
    private const string SuperAdminRoleId = "5";

    private readonly IProcessScanUseCase _processScanService;
    private readonly IGetGuardianNotificationsUseCase _guardianNotificationsService;
    private readonly IRegisterDeviceUseCase _registerDeviceService;
    private readonly IGetStudentCredentialUseCase _credentialService;
    private readonly IReportIncidentUseCase _reportIncidentService;
    private readonly IGetAlertsUseCase _getAlertsService;
    private readonly IGetIncidentsUseCase _getIncidentsService;
    private readonly IAcknowledgeAlertUseCase _acknowledgeAlertService;
    private readonly IResolveIncidentUseCase _resolveIncidentService;

    public NotificationController(
        IProcessScanUseCase processScanService,
        IGetGuardianNotificationsUseCase guardianNotificationsService,
        IRegisterDeviceUseCase registerDeviceService,
        IGetStudentCredentialUseCase credentialService,
        IReportIncidentUseCase reportIncidentService,
        IGetAlertsUseCase getAlertsService,
        IGetIncidentsUseCase getIncidentsService,
        IAcknowledgeAlertUseCase acknowledgeAlertService,
        IResolveIncidentUseCase resolveIncidentService)
    {
        _processScanService = processScanService;
        _guardianNotificationsService = guardianNotificationsService;
        _registerDeviceService = registerDeviceService;
        _credentialService = credentialService;
        _reportIncidentService = reportIncidentService;
        _getAlertsService = getAlertsService;
        _getIncidentsService = getIncidentsService;
        _acknowledgeAlertService = acknowledgeAlertService;
        _resolveIncidentService = resolveIncidentService;
    }

    [EnableRateLimiting("writes")]
    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequestDto request, CancellationToken ct)
    {
        if (User.FindFirstValue("roleId") != DriverRoleId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var driverProfileId = CurrentProfileId();
        if (driverProfileId == Guid.Empty)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _processScanService.ExecuteAsync(driverProfileId, request, ct));
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

    [EnableRateLimiting("writes")]
    [HttpPost("incidents")]
    public async Task<IActionResult> ReportIncident(
        [FromBody] ReportIncidentRequestDto request, CancellationToken ct)
    {
        if (User.FindFirstValue("roleId") != DriverRoleId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var driverProfileId = CurrentProfileId();
        if (driverProfileId == Guid.Empty)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _reportIncidentService.ExecuteAsync(driverProfileId, request, ct));
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

    [HttpGet("credential")]
    public async Task<IActionResult> GetCredential(CancellationToken ct)
    {
        var studentProfileId = CurrentProfileId();
        if (studentProfileId == Guid.Empty)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _credentialService.ExecuteAsync(studentProfileId, ct));
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

    [HttpGet("guardian")]
    public async Task<IActionResult> GetGuardianNotifications(CancellationToken ct)
    {
        var profileId = CurrentProfileId();
        if (profileId == Guid.Empty)
        {
            return Unauthorized();
        }

        return Ok(await _guardianNotificationsService.ExecuteAsync(profileId, ct));
    }

    [EnableRateLimiting("writes")]
    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequestDto request, CancellationToken ct)
    {
        var profileId = CurrentProfileId();
        if (profileId == Guid.Empty)
        {
            return Unauthorized();
        }

        await _registerDeviceService.ExecuteAsync(profileId, request, ct);
        return Ok();
    }

    private Guid CurrentProfileId() =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private bool IsAdminOrSuperAdmin() =>
        User.FindFirstValue("roleId") == AdminRoleId || User.FindFirstValue("roleId") == SuperAdminRoleId;

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
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
        if (!IsAdminOrSuperAdmin())
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
            Search = search
        };

        var result = await _getAlertsService.ExecuteAsync(filters, ct);
        return Ok(result);
    }

    [HttpPost("alerts/{alertId:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId, CancellationToken ct)
    {
        if (!IsAdminOrSuperAdmin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

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

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents(
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sort = "reportedAt,desc",
        [FromQuery] IncidentType? incidentType = null,
        [FromQuery] IncidentSeverity? severity = null,
        [FromQuery] IncidentStatus? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (!IsAdminOrSuperAdmin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var filters = new IncidentFilters
        {
            Page = page,
            Size = size,
            Sort = sort,
            IncidentType = incidentType,
            Severity = severity,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Search = search
        };

        var result = await _getIncidentsService.ExecuteAsync(filters, ct);
        return Ok(result);
    }

    [HttpPost("incidents/{incidentId:guid}/resolve")]
    public async Task<IActionResult> ResolveIncident(Guid incidentId, CancellationToken ct)
    {
        if (!IsAdminOrSuperAdmin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        try
        {
            await _resolveIncidentService.ExecuteAsync(incidentId, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
