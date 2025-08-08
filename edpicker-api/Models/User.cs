namespace edpicker_api.Models
{
    public class User
    {
        public int UserId { get; set; }  // Primary Key
        public string? GoogleId { get; set; }  // Google unique identifier
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public bool EmailVerified { get; set; } = false;
        public bool PhoneVerified { get; set; } = false;

        public string? Location { get; set; }
        public string? PreferredLocation { get; set; }
        public decimal? PreferredSalary { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }

    }
}
