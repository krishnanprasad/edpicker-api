namespace edpicker_api.Models.Dto
{
    public class DownloadPaperRequestDto
    {
        public List<QuestionPaperItemDto> Questions { get; set; } = new();
    }

    public class QuestionPaperItemDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
    }
}
