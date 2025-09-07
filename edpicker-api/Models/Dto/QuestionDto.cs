using System.Collections.Generic;
using edpicker_api.Models.Enum;

namespace edpicker_api.Models.Dto
{
    public class QuestionDto
    {
        public string QuestionId { get; set; } = Guid.NewGuid().ToString();
        public string QuestionText { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string? Answer { get; set; }
        public List<string>? Options { get; set; }
        public QuestionType? QuestionType { get; set; }
        public string? Section { get; set; }
    }
}
