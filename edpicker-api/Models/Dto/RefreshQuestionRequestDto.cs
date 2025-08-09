using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Dto
{
    public class RefreshQuestionRequestDto
    {
        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Topic { get; set; } = string.Empty;

        [Required]
        public string QuestionType { get; set; } = string.Empty;

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        [Required]
        public string OldQuestionId { get; set; } = string.Empty;
    }
}
