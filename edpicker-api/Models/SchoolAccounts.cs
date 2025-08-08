using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models
{
    public class SchoolAccounts
    {
        [Key] // This makes SchoolId the primary key
        public int SchoolId { get; set; }
        public int SchoolAccountId { get; set; }
        [Required]
        public string SchoolName { get; set; }
        
    }
}
