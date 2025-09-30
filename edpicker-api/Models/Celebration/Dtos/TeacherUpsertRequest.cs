using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Celebration.Dtos;

public class TeacherUpsertRequest
{
    public int? TeacherId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }
        = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly? Anniversary { get; set; }
        = null;

    [Required]
    [RegularExpression("^\\+91[0-9]{10}$", ErrorMessage = "Phone must be in +91XXXXXXXXXX format")]
    public string Phone { get; set; } = string.Empty;

    public bool Consent { get; set; } = true;
}
