// Models/Dto/SchoolSubjectDto.cs
namespace edpicker_api.Models.QuestionPaper.Dto
{
    public class SchoolSubjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Enabled { get; set; }
        public int ChapterCount { get; set; }
        public int TopicCount { get; set; }
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
    }
}