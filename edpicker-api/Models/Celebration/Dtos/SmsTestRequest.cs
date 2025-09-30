using System.ComponentModel.DataAnnotations;
using edpicker_api.Models.Celebration.Enums;

namespace edpicker_api.Models.Celebration.Dtos;

public class SmsTestRequest
{
    [Required]
    public int RecipientId { get; set; }

    [Required]
    public CelebrationRecipientType RecipientType { get; set; }
        = CelebrationRecipientType.Student;

    [MaxLength(180)]
    public string? Message { get; set; }
        = null;
}

public class SmsSendResultDto
{
    public bool Success { get; init; }

    public string? Error { get; init; }
        = null;
}
