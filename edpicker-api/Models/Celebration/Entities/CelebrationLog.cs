using System.ComponentModel.DataAnnotations;
using edpicker_api.Models.Celebration.Enums;

namespace edpicker_api.Models.Celebration.Entities;

/// <summary>
/// Represents an SMS delivery attempt for the celebration feature.
/// </summary>
public class CelebrationLog
{
    [Key]
    public long LogId { get; set; }

    [Required]
    public int AdminId { get; set; }

    [Required]
    public CelebrationRecipientType RecipientType { get; set; }

    public int? RecipientId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RecipientName { get; set; } = string.Empty;

    [Required]
    public CelebrationEventType EventType { get; set; }

    [Required]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public CelebrationDeliveryStatus Status { get; set; }
        = CelebrationDeliveryStatus.Sent;

    [MaxLength(250)]
    public string? FailureReason { get; set; }
        = null;

    public int Attempt { get; set; } = 1;

    public DateOnly EventDate { get; set; }
        = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset SentAt { get; set; }
        = DateTimeOffset.UtcNow;
}
