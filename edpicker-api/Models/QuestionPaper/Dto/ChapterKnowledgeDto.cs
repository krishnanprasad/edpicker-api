// Models/Dto/ChapterKnowledgeDto.cs
namespace edpicker_api.Models.QuestionPaper.Dto
{
    public class ChapterKnowledgeDto
    {
        public string VectorId { get; set; }
        public string FileId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int ChapterId { get; set; }
        public string ChapterName { get; set; }
    }
}