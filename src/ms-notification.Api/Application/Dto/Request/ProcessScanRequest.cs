using System.ComponentModel.DataAnnotations;
using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Dto.Request;

/// <summary>
/// Request DTO for processing a student scan.
/// </summary>
public sealed class ProcessScanRequest
{
    /// <summary>
    /// QR token containing student credentials.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string QrToken { get; init; } = string.Empty;

    /// <summary>
    /// Type of boarding (OnBoard or OffBoard).
    /// </summary>
    [Required]
    public BoardingType BoardingType { get; init; }
}