namespace edpicker_api.Models.Dto
{
    public class JobBoardDetailsDto
    {
        public int JobBoardId { get; set; }
        public int SchoolId { get; set; }
        public string Title { get; set; }
        public string JobDescription { get; set; }
        public string JobStatus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsDeleted { get; set; }

        // School info
        public string SchoolName { get; set; }
        public string SchoolAddress { get; set; }
        public string SchoolBoard { get; set; }

        // Location
        public string JobCity { get; set; }
        public string JobState { get; set; }

        // Compensation & experience
        public int MinExperience { get; set; }
        public int MaxExperience { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public bool? IsVerified { get; set; }

        // Contact details
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }

        // Aggregate
        public int ApplicationCount { get; set; }
    }
}
