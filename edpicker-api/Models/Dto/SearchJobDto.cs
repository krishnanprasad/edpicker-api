namespace edpicker_api.Models.Dto
{
    public class SearchJobDto
    {
        public int JobBoardId { get; set; }
        public string Title { get; set; }
        public string JobDescription { get; set; }
        public string SchoolName { get; set; }
        public string? SchoolAddress { get; set; }
        public string SchoolBoard { get; set; }
        public string JobCity { get; set; }
        public string JobState { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public bool? IsVerified { get; set; }
        public string JobStatus { get; set; }
        public double? DistanceMeters { get; set; }
    }
}
