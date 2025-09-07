using System;

namespace edpicker_api.Models.Job
{
    public class JobBoard
    {
        public int JobBoardId { get; set; }
        public int SchoolId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public int MinExperience { get; set; }
        public int MaxExperience { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public bool? IsVerified { get; set; } // <-- Make sure this is present and matches DB

        // Navigation properties
        public School School { get; set; }
        public JobBoardContactDetails ContactDetails { get; set; }
    }
}
