namespace edpicker_api.Models.Dto
{
    public class School_JobApplicationDto
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public int UserId { get; set; }
        public DateTime AppliedDate { get; set; }
        public int ApplicationStatusId { get; set; }
        public string ApplicationStatus { get; set; }
        public DateTime StatusDate { get; set; }
        public string Notes { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public string FullName { get; set; }
        public string ResumeUrl { get; set; }
        public string Email { get; set; }
    }
}
