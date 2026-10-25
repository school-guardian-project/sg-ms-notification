using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Auth;

namespace ms_notification.Api.Infrastructure.Controller;

[ApiController]
[Route("api/v1/notifications")]
public class CredentialController : ControllerBase
{
    private readonly IGetStudentCredentialUseCase _credentialService;

    public CredentialController(IGetStudentCredentialUseCase credentialService)
    {
        _credentialService = credentialService;
    }

    [HttpGet("credential")]
    [RequireProfile]
    public async Task<IActionResult> GetCredential(CancellationToken ct)
    {
        try
        {
            return Ok(await _credentialService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), ct));
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
