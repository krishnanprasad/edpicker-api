using System.ComponentModel.DataAnnotations;
using edpicker_api.Models.Celebration.Enums;

namespace edpicker_api.Models.Celebration.Entities;

/// <summary>
/// Tracks user actions within the celebration module for compliance.
/// </summary>
public class CelebrationAuditLog
{
    [Key]
    public long AuditId { get; set; }

    [Required]
    public int AdminId { get; set; }

    [Required]
    public CelebrationAuditAction Action { get; set; }
        = CelebrationAuditAction.Upload;

    [MaxLength(400)]
    public string? Details { get; set; }
        = null;

    public DateTimeOffset Timestamp { get; set; }
        = DateTimeOffset.UtcNow;
}
