using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace ms_notification.Api.Infrastructure.Security;

public class JwtValidationFilter : IAuthorizationFilter
{
    private readonly JwksCache _jwksCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtValidationFilter> _logger;

    public JwtValidationFilter(JwksCache jwksCache, IConfiguration configuration, ILogger<JwtValidationFilter> logger)
    {
        _jwksCache = jwksCache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ")) return;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var kid = jwtToken.Header.Kid;
            var publicKey = await _jwksCache.GetPublicKeyAsync(kid);

            if (publicKey == null) throw new Exception($"Public key for kid {kid} not found");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "school-guardian-iam",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "school-guardian-api",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(GetRsaKeyFromJwk(publicKey))
            };

            handler.ValidateToken(token, validationParameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Local JWT validation failed: {Message}", ex.Message);
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
        }
    }

    private System.Security.Cryptography.RSA GetRsaKeyFromJwk(string publicKeyData) { return new System.Security.Cryptography.RSA(); }
}
