using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Auth;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class GuardianNotificationController : ControllerBase
{
    private readonly IGetGuardianNotificationsUseCase _guardianNotificationsService;

    public GuardianNotificationController(IGetGuardianNotificationsUseCase guardianNotificationsService)
    {
        _guardianNotificationsService = guardianNotificationsService;
    }

    [HttpGet("guardian")]
    [RequireProfile]
    public async Task<IActionResult> GetGuardianNotifications(CancellationToken ct)
    {
        return Ok(await _guardianNotificationsService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), ct));
    }
}
