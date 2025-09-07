namespace edpicker_api.Models.Dto
{
    public class CreateJobRequest
    {
        public int SchoolId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public int? ContactId { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public bool IsVerified { get; set; }
        public int? JobBoardStatusId { get; set; }  // Update property name to match SP
        public string Benefits { get; set; }         // NEW: for JobDetails
        public string Expectation { get; set; }      // NEW: for JobDetails
        public string Education { get; set; }
    }
}
