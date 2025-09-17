using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using edpicker_api.Models.Enum;

namespace edpicker_api.Models.Dto
{
    public class GenerateQuestionsRequestDto
    {
        // Make Class and Chapter optional if you don't want to provide them
        public string Class { get; set; } = "12"; // Default value

        [Required]
        public string Subject { get; set; } = string.Empty;

        public string Chapter { get; set; } = string.Empty; // Make optional

        [Required]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("questionTypes")] // This matches your JSON
        public List<QuestionTypeRequestDto> QuestionTypes { get; set; } = new();

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        // Optional - for backward compatibility
        public int NumberOfQuestions { get; set; }

        [JsonIgnore]
        public QuestionType QuestionType { get; set; }

        [JsonIgnore]
        public string Section { get; set; } = "any";
        public int SubjectId { get; set; }
        public int ChapterId { get; set; }
        public int? SchoolId { get; set; }
        public int? UserId { get; set; }
        public string? BrowserIp { get; set; }
    }
}
