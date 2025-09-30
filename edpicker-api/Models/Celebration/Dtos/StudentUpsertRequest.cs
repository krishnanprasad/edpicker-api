using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Celebration.Dtos;

public class StudentUpsertRequest
{
    public int? StudentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }
        = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required]
    [RegularExpression("^\\+91[0-9]{10}$", ErrorMessage = "Phone must be in +91XXXXXXXXXX format")] 
    public string Phone { get; set; } = string.Empty;

    public bool Consent { get; set; } = true;
}
