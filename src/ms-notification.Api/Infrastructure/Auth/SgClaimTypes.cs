namespace ms_notification.Api.Infrastructure.Auth;

/// <summary>
/// Nombres de los claims que ms-iam emite en el access token. Se leen tal cual
/// (sin el mapeo a ClaimTypes de .NET) para que coincidan con el JWT.
/// </summary>
public static class SgClaimTypes
{
    public const string Subject = "sub";
    public const string PersonId = "personId";
    public const string RoleId = "roleId";
    public const string Role = "role";
    public const string Email = "email";
    public const string CampusId = "campusId";
    public const string SchoolId = "schoolId";

    /// <summary>Un claim por permiso concedido (FR-2.1).</summary>
    public const string Permissions = "permissions";

    /// <summary>"access" o "refresh": ambos van firmados con la misma llave.</summary>
    public const string TokenType = "type";

    public const string AccessTokenType = "access";
}
