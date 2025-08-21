using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using edpicker_api.Models.Enum;

namespace edpicker_api.Models.Dto
{
    public class GenerateQuestionsRequestDto
    {
        [Required]
        public string Class { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Chapter { get; set; } = string.Empty;

        [Required]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public QuestionType QuestionType { get; set; }

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int NumberOfQuestions { get; set; }
    }
}
