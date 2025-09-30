using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Celebration.Dtos;

public class SchoolConfigRequest
{
    [Required]
    [MaxLength(120)]
    public string SchoolName { get; set; } = string.Empty;
}

public class SchoolConfigResponse
{
    public string SchoolName { get; init; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; init; }
        = DateTimeOffset.UtcNow;
}
