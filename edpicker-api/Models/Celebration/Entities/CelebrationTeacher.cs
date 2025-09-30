using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Celebration.Entities;

/// <summary>
/// Represents a teacher profile that can receive celebration reminders.
/// </summary>
public class CelebrationTeacher
{
    [Key]
    public int TeacherId { get; set; }

    [Required]
    public int AdminId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }
        = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly? Anniversary { get; set; }
        = null;

    [Required]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    public bool Consent { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
        = null;
}
