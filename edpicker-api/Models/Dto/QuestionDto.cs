namespace edpicker_api.Models.Dto
{
    public class QuestionDto
    {
        public string QuestionId { get; set; } = Guid.NewGuid().ToString();
        public string QuestionText { get; set; } = string.Empty;
        public string? Hint { get; set; }
    }
}
