namespace edpicker_api.Models.Dto
{
    public class UserJobApplicationDto
    {
        public int JobApplicationId { get; set; }
        public int JobBoardId { get; set; }
        public string JobTitle { get; set; }
        public string SchoolName { get; set; }
        public string Location { get; set; }
        public DateTime AppliedDate { get; set; }
        public string ApplicationStatus { get; set; }
    }
}
