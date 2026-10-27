using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using ms_notification.Api.Application.Dto.Response;
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

    // Solo un estudiante tiene ruta y parada asignadas: su QR es la credencial
    // que el conductor escanea (FR-4.1).
    [HttpGet("credential")]
    [RequireRole(RequireRoleAttribute.StudentRoleId)]
    [RequireProfile]
    public async Task<ActionResult<CredentialResponse>> GetCredential(CancellationToken ct)
    {
        try
        {
            var response = await _credentialService.ExecuteAsync(NotificationClaims.CurrentProfileId(User), ct);
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