using System.ComponentModel.DataAnnotations;

namespace ms_notification.Api.Application.Dto.Request;

/// <summary>
/// Request DTO for reporting an incident (FR-8.1).
/// The type comes from the Notification.AlertType catalog; description is optional.
/// </summary>
public sealed class ReportIncidentRequest
{
    /// <summary>
    /// Alert type identifier from the catalog.
    /// </summary>
    [Required]
    [Range(1, byte.MaxValue)]
    public byte AlertTypeId { get; init; }

    /// <summary>
    /// Optional description of the incident.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; init; }
}