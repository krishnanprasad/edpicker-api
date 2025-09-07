// Models/Dto/SubjectChapterDto.cs
namespace edpicker_api.Models.QuestionPaper.Dto
{
    public class SubjectChapterDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
    }
}