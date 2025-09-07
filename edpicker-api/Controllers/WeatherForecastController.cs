using edpicker_api.Models.Methods;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            string openAIApiKey = _configuration["OpenAIKey"];


            // 2) The file ID from your uploaded knowledge base 
            //    (for example, "file-RaA9DXMQi4exmMQtktuExA" for SCHOOL001)
            string schoolFileId = "file-RaA9DXMQi4exmMQtktuExA";

            // 3) A user question about the school
            string userQuestion = "What is the admission fee?";

            var openAiHelper = new OpenAISearchMethod(openAIApiKey);

            // Step A: Search the file for the best matching line
            // (Remember, we only get doc index, not the snippet text by default.)
            string snippet = await openAiHelper.GetAnswerFromGPTAsync(schoolFileId, userQuestion);

            // Step B: Call GPT with the snippet and user question to generate a final answer
            string finalAnswer = await openAiHelper.GetAnswerFromGPTAsync(snippet, userQuestion);

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        //[HttpGet(Name = "TestOpenAI-FileID")]
        //public async Task<ActionResult> TestFileID()
        //{
        //    var rng = new Random();
        //    //string openAIApiKey = "YOUR_OPENAI_API_KEY";
        //    string openAIApiKey = _configuration["OpenAIKey"];


        //    // 2) The file ID from your uploaded knowledge base 
        //    //    (for example, "file-RaA9DXMQi4exmMQtktuExA" for SCHOOL001)
        //    string schoolFileId = "file-RaA9DXMQi4exmMQtktuExA";

        //    // 3) A user question about the school
        //    string userQuestion = "What is the admission fee?";

        //    var openAiHelper = new OpenAISearchMethod(openAIApiKey);

        //    // Step A: Search the file for the best matching line
        //    // (Remember, we only get doc index, not the snippet text by default.)
        //    string snippet = await openAiHelper.GetRelevantSnippetAsync(schoolFileId, userQuestion);

        //    // Step B: Call GPT with the snippet and user question to generate a final answer
        //    string finalAnswer = await openAiHelper.GetAnswerFromGPTAsync(snippet, userQuestion);

        //    return Ok();
        //}
    }
}
