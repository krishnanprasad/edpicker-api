namespace edpicker_api.Models.Dto
{
    public class School_JobApplicationsWithCountsDto
    {
        public List<School_JobApplicationDto> Applications { get; set; } = new();
        public List<School_ApplicationStatusCountDto> StatusCounts { get; set; } = new();
    }
}
