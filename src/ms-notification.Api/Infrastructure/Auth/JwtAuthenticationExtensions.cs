using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ms_notification.Api.Infrastructure.Auth;

public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Valida en este servicio el access token RS256 que emite ms-iam: firma
    /// contra su JWKS, issuer, audience, expiracion y tipo "access". El gateway
    /// tambien lo valida, pero el servicio no confia en eso: la identidad sale
    /// del token, no de las cabeceras X-User-*.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtAuthenticationOptions>()
            .Bind(configuration.GetSection(JwtAuthenticationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwt = configuration.GetSection(JwtAuthenticationOptions.SectionName)
                      .Get<JwtAuthenticationOptions>()
                  ?? new JwtAuthenticationOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Conserva los nombres del JWT ("sub", "roleId", "permissions"...).
                options.MapInboundClaims = false;
                options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    jwt.JwksUrl,
                    new JwksConfigurationRetriever(),
                    new HttpDocumentRetriever { RequireHttps = jwt.JwksUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) });

                options.TokenValidationParameters = CreateTokenValidationParameters(jwt);

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (!IsAccessToken(context.Principal))
                        {
                            context.Fail("Only access tokens are accepted.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Reglas de validacion del access token. Las llaves de firma no van aqui:
    /// las aporta el ConfigurationManager desde el JWKS de ms-iam.
    /// </summary>
    public static TokenValidationParameters CreateTokenValidationParameters(JwtAuthenticationOptions jwt) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidateIssuerSigningKey = true,
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
        NameClaimType = SgClaimTypes.Subject,
        RoleClaimType = SgClaimTypes.Role,
    };

    /// <summary>
    /// El refresh token tambien es un JWT firmado con la misma llave, issuer y
    /// audience; solo el claim "type" lo distingue. Sin esta comprobacion un
    /// refresh token (7 dias) serviria como access token.
    /// </summary>
    public static bool IsAccessToken(ClaimsPrincipal? principal) =>
        principal?.FindFirst(SgClaimTypes.TokenType)?.Value == SgClaimTypes.AccessTokenType;
}
