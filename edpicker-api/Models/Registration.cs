namespace edpicker_api.Models
{
    public class Registration
    {
        public string Id { get; set; }
        public string StatusId { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailId { get; set; }
        public string Message { get; set; }
        public MessageType MessageType { get; set; }
        public string SchoolId { get; set; }
        public string Status { get; set; }
    }

    public enum MessageType
    {
        ContactUs = 1,
        SchoolRegistration = 2
    }
}
