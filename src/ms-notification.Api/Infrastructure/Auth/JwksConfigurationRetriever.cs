using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// ms-iam no publica un documento OpenID Connect discovery, solo el JWKS. Este
/// retriever lee el JWKS y lo expone como una configuracion con sus llaves de
/// firma, para que el ConfigurationManager de JwtBearer lo cachee y lo recargue
/// cuando llega un token firmado con un kid desconocido (rotacion de llaves).
/// </summary>
public sealed class JwksConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var document = await retriever.GetDocumentAsync(address, cancel);
        var keySet = new JsonWebKeySet(document);

        var configuration = new OpenIdConnectConfiguration();
        foreach (var key in keySet.GetSigningKeys())
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }
}
