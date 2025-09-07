namespace edpicker_api.Models
{
    public class Registration_Post
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailId { get; set; }
        public string Message { get; set; }
        public MessageType MessageType { get; set; }
        public string SchoolId { get; set; }
        
    }
}
