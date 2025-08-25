using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Collections.Generic;
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
        [JsonPropertyName("questionType")]
        public List<QuestionTypeRequestDto> QuestionTypes { get; set; } = new();

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        [JsonIgnore]
        public QuestionType QuestionType { get; set; }

        [JsonIgnore]
        public int NumberOfQuestions { get; set; }

        [JsonIgnore]
        public string Section { get; set; } = "any";
    }
}
