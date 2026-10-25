using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Auth;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class DeviceController : ControllerBase
{
    private readonly IRegisterDeviceUseCase _registerDeviceService;

    public DeviceController(IRegisterDeviceUseCase registerDeviceService)
    {
        _registerDeviceService = registerDeviceService;
    }

    [EnableRateLimiting("writes")]
    [HttpPost("devices")]
    [RequireProfile]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequestDto request, CancellationToken ct)
    {
        await _registerDeviceService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), request, ct);
        return Ok();
    }
}
