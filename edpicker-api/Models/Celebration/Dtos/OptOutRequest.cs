using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Celebration.Dtos;

public class OptOutRequest
{
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Message { get; set; }
        = null;
}

public class OptOutResponse
{
    public bool Updated { get; init; }
}
