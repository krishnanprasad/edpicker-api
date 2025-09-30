using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Celebration.Entities;

/// <summary>
/// Stores per-admin configuration for celebration messaging.
/// </summary>
public class CelebrationConfiguration
{
    [Key]
    public int AdminId { get; set; }

    [Required]
    [MaxLength(120)]
    public string SchoolName { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}
