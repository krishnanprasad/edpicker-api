using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using edpicker_api.Models.Enum;

namespace edpicker_api.Models.Dto
{
    public class RefreshQuestionRequestDto
    {
        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public QuestionType QuestionType { get; set; }

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        [Required]
        public string OldQuestionId { get; set; } = string.Empty;
    }
}
