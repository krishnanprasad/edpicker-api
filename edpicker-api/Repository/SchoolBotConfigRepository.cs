namespace edpicker_api.Repository
{
    public static class SchoolBotConfigRepository
    {
        public static Dictionary<string, SchoolBotConfig> Configs { get; } = new()
        {
        {
            "SCHOOL001",
            new SchoolBotConfig
            {
                SchoolId = "SCHOOL001",
                GreetingMessage = "Hello from School 001!",
                // other fields...
                PhoneNumberId = "",  // optional if not used
                AnswerFormat = "Bullet points",
                AnswerLength = 50,
                OpenAIFileId = "file-RaA9DXMQi4exmMQtktuExA",
                Instruction = "Be formal and concise"
            }
        },
        {
            "SCHOOL002",
            new SchoolBotConfig
            {
                SchoolId = "SCHOOL002",
                GreetingMessage = "Welcome to School 002!",
                // other fields...
                PhoneNumberId = "",
                AnswerFormat = "Plain text",
                AnswerLength = 100,
                OpenAIFileId = "file-AbC123XyZ",
                Instruction = "Use a friendly tone"
            }
        }
        };
    }
    public class SchoolBotConfig
{
    public string SchoolId { get; set; }
    public string PhoneNumberId { get; set; }
    public string GreetingMessage { get; set; }
    public string AnswerFormat { get; set; }
    public int AnswerLength { get; set; }
    public string OpenAIFileId { get; set; }
    public string Instruction { get; set; }
}
}
