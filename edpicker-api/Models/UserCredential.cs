namespace edpicker_api.Models
{
    public class UserCredential
    {
        public string UserId { get; set; }
        public int SchoolId { get; set; }
        public string PasswordHash { get; set; }
        public DateTime ExpiryDateUtc { get; set; }
        public bool IsDeleted { get; set; }
        public string SchoolName { get; set; }
    }
}
