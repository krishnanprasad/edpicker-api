using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Dto
{
    public class School_UpdateSchoolAccountDto
    {
        
        public string ContactName { get; set; }

        public string? ContactAddress { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits")]
        public string ContactPincode { get; set; }

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact number must be exactly 10 digits")]
        public string PrimaryPhone { get; set; }

        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact number must be exactly 10 digits")]
        public string? SecondaryPhone { get; set; }

        public string? SchoolName { get; set; } // Optional; only update if provided

    }
}
