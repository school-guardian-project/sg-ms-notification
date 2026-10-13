using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Port.In;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private const string DriverRoleId = "3";

    private readonly IProcessScanUseCase _processScanService;
    private readonly IGetGuardianNotificationsUseCase _guardianNotificationsService;
    private readonly IRegisterDeviceUseCase _registerDeviceService;
    private readonly IGetStudentCredentialUseCase _credentialService;

    public NotificationController(
        IProcessScanUseCase processScanService,
        IGetGuardianNotificationsUseCase guardianNotificationsService,
        IRegisterDeviceUseCase registerDeviceService,
        IGetStudentCredentialUseCase credentialService)
    {
        _processScanService = processScanService;
        _guardianNotificationsService = guardianNotificationsService;
        _registerDeviceService = registerDeviceService;
        _credentialService = credentialService;
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
            // Regla de negocio (sin viaje, sin asignacion, token invalido): 409.
            return Conflict(new { message = ex.Message });
        }
        catch (RpcException)
        {
            // No se pudo verificar contra otro servicio: 503, no 500.
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
}
