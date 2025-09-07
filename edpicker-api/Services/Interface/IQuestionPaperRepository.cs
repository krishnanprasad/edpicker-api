using edpicker_api.Models.Dto;
using edpicker_api.Models.QuestionPaper.Dto;

namespace edpicker_api.Services.Interface
{
    public interface IQuestionPaperRepository
    {
        Task<IEnumerable<QuestionDto>> GenerateQuestionsAsync(GenerateQuestionsRequestDto request);
        Task<IEnumerable<QuestionDto>> GenerateQuestionsWithResponsesAsync(GenerateQuestionsRequestDto request);
        Task<QuestionDto> RefreshQuestionAsync(RefreshQuestionRequestDto request);
        Task<byte[]> GenerateQuestionPaperAsync(DownloadPaperRequestDto request);
        Task<IEnumerable<SchoolClassDto>> GetSchoolClassesAsync(int schoolId);
        Task<IEnumerable<SchoolSubjectDto>> GetSchoolSubjectsAsync(int schoolId, int classId);
        Task<IEnumerable<TopicDto>> GetTopicsBySubjectForSchoolAsync(int schoolId, int subjectId,int chapterId);
        Task<IEnumerable<SubjectChapterDto>> GetSubjectChaptersBySubjectAsync(int subjectId);


    }
}
