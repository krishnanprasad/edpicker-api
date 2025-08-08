namespace edpicker_api.Models.Job
{
    public class JobDetails
    {
        public int JobBoardId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public int MinExperience { get; set; }
        public int MaxExperience { get; set; }
        public decimal MinSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public bool IsVerified { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
