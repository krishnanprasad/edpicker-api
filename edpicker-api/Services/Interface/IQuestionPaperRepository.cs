using edpicker_api.Models.Dto;

namespace edpicker_api.Services.Interface
{
    public interface IQuestionPaperRepository
    {
        Task<IEnumerable<QuestionDto>> GenerateQuestionsAsync(GenerateQuestionsRequestDto request);
        Task<QuestionDto> RefreshQuestionAsync(RefreshQuestionRequestDto request);
        Task<byte[]> GenerateQuestionPaperAsync(DownloadPaperRequestDto request);
    }
}
