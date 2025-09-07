namespace edpicker_api.Models
{
    public class UserCredential
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int SchoolId { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public string SchoolName { get; set; }
    }
}
