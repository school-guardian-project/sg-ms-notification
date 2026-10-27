using System.ComponentModel.DataAnnotations;

namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Parametros para validar localmente el access token RS256 emitido por ms-iam.
/// Se configuran con la seccion "Jwt" (variables Jwt__Issuer, Jwt__Audience,
/// Jwt__JwksUrl); los valores por defecto son los del despliegue.
/// </summary>
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "school-guardian-iam";

    [Required]
    public string Audience { get; init; } = "school-guardian-api";

    /// <summary>JWKS publicado por ms-iam dentro de la red de Docker.</summary>
    [Required]
    public string JwksUrl { get; init; } = "http://ms-iam:8080/.well-known/jwks.json";

    /// <summary>Tolerancia de reloj entre ms-iam y este servicio.</summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; init; } = 30;
}
