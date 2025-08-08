namespace edpicker_api.Models.Dto
{
    public class School_GetProfileDto
    {
        public int SchoolId { get; set; }
        public string SchoolName { get; set; }
        public string BoardName { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactPincode { get; set; }
        public string PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string ContactName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
