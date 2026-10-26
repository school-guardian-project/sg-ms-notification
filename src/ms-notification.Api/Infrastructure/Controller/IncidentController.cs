using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Security;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class IncidentController : ControllerBase
{
    private readonly IGetIncidentsUseCase _getIncidentsService;
    private readonly IResolveIncidentUseCase _resolveIncidentService;
    private readonly IGetIncidentByIdUseCase _getIncidentByIdService;

    public IncidentController(
        IGetIncidentsUseCase getIncidentsService,
        IResolveIncidentUseCase resolveIncidentService,
        IGetIncidentByIdUseCase getIncidentByIdService)
    {
        _getIncidentsService = getIncidentsService;
        _resolveIncidentService = resolveIncidentService;
        _getIncidentByIdService = getIncidentByIdService;
    }

    [HttpGet("incidents")]
    [RequireRole(RequireRoleAttribute.AdminRoleId, RequireRoleAttribute.SuperAdminRoleId)]
    public async Task<ActionResult<ms_notification.Api.Application.Dto.Response.IncidentsResponse>> GetIncidents(
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
    [RequireRole(RequireRoleAttribute.AdminRoleId, RequireRoleAttribute.SuperAdminRoleId)]
    public async Task<IActionResult> ResolveIncident(Guid incidentId, CancellationToken ct)
    {
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

    [HttpGet("incidents/{incidentId:guid}")]
    [RequireRole(RequireRoleAttribute.AdminRoleId, RequireRoleAttribute.SuperAdminRoleId)]
    public async Task<ActionResult<ms_notification.Api.Application.Dto.Response.IncidentListResponse>> GetIncidentById(Guid incidentId, CancellationToken ct)
    {
        var incident = await _getIncidentByIdService.ExecuteAsync(incidentId, ct);
        if (incident is null)
        {
            return NotFound();
        }

        return Ok(incident);
    }
}