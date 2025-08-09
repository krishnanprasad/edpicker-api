using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Dto
{
    public class GenerateQuestionsRequestDto
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
        [Range(1, int.MaxValue)]
        public int NumberOfQuestions { get; set; }
    }
}
