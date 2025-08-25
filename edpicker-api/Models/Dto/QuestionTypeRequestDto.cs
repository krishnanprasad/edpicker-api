using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using edpicker_api.Models.Enum;

namespace edpicker_api.Models.Dto
{
    public class QuestionTypeRequestDto
    {
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("questionType")] // This matches your JSON
        public QuestionType Type { get; set; }

        public string Section { get; set; } = "any";

        [Required]
        [Range(1, int.MaxValue)]
        public int NumberOfQuestions { get; set; }
    }
}
