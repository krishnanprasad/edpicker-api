using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Dto
{
    public class SchoolAccountDto
    {
        [Required(ErrorMessage = "School name is required")]
        public string SchoolName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be 10 digits")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6)]
        public string Password { get; set; }

        [Required(ErrorMessage = "City is required")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "State is required")]
        public int StateId { get; set; }

        [Required(ErrorMessage = "Board is required")]
        public int BoardId { get; set; }
    }
}
