using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using ms_notification.Api.Infrastructure.Auth;
using NUnit.Framework;

namespace ms_notification.Tests;

public class JwtAuthenticationTests
{
    private const string AdminRoleId = "1";
    private const string StudentRoleId = "2";
    private const string DriverRoleId = "3";

    private static readonly JwtAuthenticationOptions Options = new();

    private RSA _iamKey = null!;
    private RsaSecurityKey _signingKey = null!;

    [SetUp]
    public void SetUp()
    {
        _iamKey = RSA.Create(2048);
        _signingKey = new RsaSecurityKey(_iamKey) { KeyId = "iam-key-1" };
    }

    [TearDown]
    public void TearDown() => _iamKey.Dispose();

    [Test]
    public async Task AccessTokenValido_SeAcepta_YConservaLosClaimsDelJwt()
    {
        var result = await ValidateAsync(CreateToken(), _signingKey);

        Assert.That(result.IsValid, Is.True, result.Exception?.Message);
        var principal = new ClaimsPrincipal(result.ClaimsIdentity);
        Assert.That(JwtAuthenticationExtensions.IsAccessToken(principal), Is.True);
        Assert.That(principal.FindFirstValue(SgClaimTypes.RoleId), Is.EqualTo(DriverRoleId));
        Assert.That(principal.HasClaim(SgClaimTypes.Permissions, "boarding.manage"), Is.True);
    }

    [Test]
    public async Task RefreshToken_FirmaValida_NoEsAccessToken()
    {
        var result = await ValidateAsync(CreateToken(type: "refresh"), _signingKey);

        // La firma es valida (misma llave), pero el tipo lo descarta.
        Assert.That(result.IsValid, Is.True);
        Assert.That(JwtAuthenticationExtensions.IsAccessToken(new ClaimsPrincipal(result.ClaimsIdentity)), Is.False);
    }

    [Test]
    public void TokenSinClaimType_NoEsAccessToken()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(SgClaimTypes.Subject, Guid.NewGuid().ToString())], "test"));

        Assert.That(JwtAuthenticationExtensions.IsAccessToken(principal), Is.False);
        Assert.That(JwtAuthenticationExtensions.IsAccessToken(null), Is.False);
    }

    [Test]
    public async Task FirmadoConOtraLlave_SeRechaza()
    {
        using var otherKey = RSA.Create(2048);
        var token = CreateToken(signingKey: new RsaSecurityKey(otherKey) { KeyId = "iam-key-1" });

        var result = await ValidateAsync(token, _signingKey);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task AudienceDistinta_SeRechaza()
    {
        var result = await ValidateAsync(CreateToken(audience: "otra-api"), _signingKey);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task IssuerDistinto_SeRechaza()
    {
        var result = await ValidateAsync(CreateToken(issuer: "otro-emisor"), _signingKey);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task TokenExpirado_SeRechaza()
    {
        var result = await ValidateAsync(CreateToken(expires: DateTime.UtcNow.AddMinutes(-5)), _signingKey);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task TokenFirmadoConHmac_SeRechaza()
    {
        var hmacKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = "iam-key-1" };
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Options.Issuer,
            Audience = Options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object> { [SgClaimTypes.TokenType] = "access" },
            SigningCredentials = new SigningCredentials(hmacKey, SecurityAlgorithms.HmacSha256),
        });

        var result = await ValidateAsync(token, hmacKey);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task JwksDeIam_SeConvierteEnLlavesDeFirma()
    {
        var parameters = _iamKey.ExportParameters(false);
        var jwks = $$"""
            {"keys":[{"kty":"RSA","use":"sig","alg":"RS256","kid":"iam-key-1",
              "n":"{{Base64UrlEncoder.Encode(parameters.Modulus)}}",
              "e":"{{Base64UrlEncoder.Encode(parameters.Exponent)}}"}]}
            """;

        var configuration = await new JwksConfigurationRetriever()
            .GetConfigurationAsync("http://ms-iam:8080/.well-known/jwks.json", new FixedDocumentRetriever(jwks), CancellationToken.None);

        Assert.That(configuration.SigningKeys, Has.Count.EqualTo(1));
        var key = configuration.SigningKeys.Single();
        Assert.That(key.KeyId, Is.EqualTo("iam-key-1"));

        // Con la llave leida del JWKS se valida un token firmado por ms-iam.
        var result = await ValidateAsync(CreateToken(), key);
        Assert.That(result.IsValid, Is.True, result.Exception?.Message);
    }

    [Test]
    public async Task RequirePermission_SinToken_Devuelve401()
    {
        var context = FilterContext(new ClaimsPrincipal(new ClaimsIdentity()));

        await new RequirePermissionAttribute("alerts.ack").OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    [Test]
    public async Task RequirePermission_SinElPermiso_Devuelve403()
    {
        var context = FilterContext(Authenticated(permissions: ["alerts.read"]));

        await new RequirePermissionAttribute("alerts.ack").OnAuthorizationAsync(context);

        Assert.That((context.Result as ObjectResult)?.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task RequirePermission_ConElPermiso_DejaPasar()
    {
        var context = FilterContext(Authenticated(permissions: ["alerts.ack"]));

        await new RequirePermissionAttribute("alerts.ack").OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task RequireRole_SinToken_Devuelve401()
    {
        var context = FilterContext(new ClaimsPrincipal(new ClaimsIdentity()));

        await new RequireRoleAttribute(DriverRoleId).OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Credencial_ConductorNoPuedePedirla_Devuelve403()
    {
        var context = FilterContext(Authenticated(roleId: DriverRoleId));

        await new RequireRoleAttribute(StudentRoleId).OnAuthorizationAsync(context);

        Assert.That((context.Result as StatusCodeResult)?.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task Credencial_EstudiantePuedePedirla()
    {
        var context = FilterContext(Authenticated(roleId: StudentRoleId));

        await new RequireRoleAttribute(StudentRoleId).OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task RequireProfile_SubInvalido_Devuelve401()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(SgClaimTypes.Subject, "no-es-guid")], "Bearer"));
        var context = FilterContext(principal);

        await new RequireProfileAttribute().OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    private string CreateToken(
        string type = "access",
        string? issuer = null,
        string? audience = null,
        DateTime? expires = null,
        SecurityKey? signingKey = null)
    {
        var expiry = expires ?? DateTime.UtcNow.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Options.Issuer,
            Audience = audience ?? Options.Audience,
            IssuedAt = expiry.AddMinutes(-60),
            NotBefore = expiry.AddMinutes(-60),
            Expires = expiry,
            Claims = new Dictionary<string, object>
            {
                [SgClaimTypes.Subject] = Guid.NewGuid().ToString(),
                [SgClaimTypes.PersonId] = Guid.NewGuid().ToString(),
                [SgClaimTypes.RoleId] = 3,
                [SgClaimTypes.TokenType] = type,
                [SgClaimTypes.Permissions] = new[] { "boarding.read", "boarding.manage" },
            },
            SigningCredentials = new SigningCredentials(signingKey ?? _signingKey, SecurityAlgorithms.RsaSha256),
        });
    }

    private static Task<TokenValidationResult> ValidateAsync(string token, SecurityKey key)
    {
        var parameters = JwtAuthenticationExtensions.CreateTokenValidationParameters(Options);
        parameters.IssuerSigningKey = key;
        return new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
    }

    private static ClaimsPrincipal Authenticated(string roleId = AdminRoleId, string[]? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(SgClaimTypes.Subject, Guid.NewGuid().ToString()),
            new(SgClaimTypes.RoleId, roleId),
        };
        claims.AddRange((permissions ?? []).Select(p => new Claim(SgClaimTypes.Permissions, p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static AuthorizationFilterContext FilterContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());
    }

    private sealed class FixedDocumentRetriever(string document) : IDocumentRetriever
    {
        public Task<string> GetDocumentAsync(string address, CancellationToken cancel) => Task.FromResult(document);
    }
}
