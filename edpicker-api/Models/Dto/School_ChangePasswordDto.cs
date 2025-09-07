using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Dto
{
    public class School_ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; }

        [Required, MinLength(6)]
        public string NewPassword { get; set; }

        [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
