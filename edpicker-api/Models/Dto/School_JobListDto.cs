namespace edpicker_api.Models.Dto
{
    public class School_JobListDto
    {
        public int JobBoardId { get; set; }
        public int SchoolId { get; set; }
        public string Title { get; set; }
        public string JobDescription { get; set; }
        public string JobStatus { get; set; }
        public string JobCity { get; set; }
        public string JobState { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public bool? IsVerified { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
