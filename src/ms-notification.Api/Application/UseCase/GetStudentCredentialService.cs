using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Security;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Api.Application.UseCase;

public class GetStudentCredentialService : IGetStudentCredentialUseCase
{
    private const string DefaultStudentName = "Estudiante";

    private readonly IRouteDirectory _routeDirectory;
    private readonly IStudentDirectory _studentDirectory;
    private readonly QrTokenSigner _signer;

    public GetStudentCredentialService(
        IRouteDirectory routeDirectory,
        IStudentDirectory studentDirectory,
        QrTokenSigner signer)
    {
        _routeDirectory = routeDirectory;
        _studentDirectory = studentDirectory;
        _signer = signer;
    }

    public async Task<CredentialResponseDto> ExecuteAsync(Guid studentProfileId, CancellationToken ct)
    {
        var route = await _routeDirectory.GetStudentRouteAsync(studentProfileId, ct)
                    ?? throw new InvalidOperationException("Student has no active route assigned");

        var stop = await _routeDirectory.GetStudentStopOnRouteAsync(route.RouteId, studentProfileId, ct)
                   ?? throw new InvalidOperationException("Student is not assigned to a stop on their route");

        var resolved = await _studentDirectory.GetStudentNameAsync(studentProfileId, ct);
        var name = string.IsNullOrWhiteSpace(resolved) ? DefaultStudentName : resolved;
        var issued = _signer.Issue(studentProfileId, name, route.RouteId, stop.RouteStopId);

        return new CredentialResponseDto
        {
            QrToken = issued.Token,
            ExpiresAt = issued.ExpiresAt
        };
    }
}
