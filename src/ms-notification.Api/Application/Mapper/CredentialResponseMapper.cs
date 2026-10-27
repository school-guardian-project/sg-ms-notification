using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

/// <summary>
/// Maps Credential domain entities to Response DTOs.
/// This mapper is stateless and can be registered as a singleton.
/// </summary>
public interface ICredentialResponseMapper
{
    CredentialResponse ToResponse(string qrToken, DateTimeOffset expiresAt);
}

public sealed class CredentialResponseMapper : ICredentialResponseMapper
{
    public CredentialResponse ToResponse(string qrToken, DateTimeOffset expiresAt)
    {
        return new CredentialResponse(qrToken, expiresAt);
    }
}