using edpicker_api.Models.Dto;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace edpicker_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionPaperController : ControllerBase
    {
        private readonly IQuestionPaperRepository _repository;
        private readonly ILogger<QuestionPaperController> _logger;

        public QuestionPaperController(IQuestionPaperRepository repository, ILogger<QuestionPaperController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpPost("generate-questions")]
        public async Task<IActionResult> GenerateQuestions([FromBody] GenerateQuestionsRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var questions = await _repository.GenerateQuestionsAsync(request);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate questions");
                return StatusCode(500, "Error generating questions");
            }
        }

        [HttpPost("refresh-question")]
        public async Task<IActionResult> RefreshQuestion([FromBody] RefreshQuestionRequestDto request)
        {
            try
            {
                var question = await _repository.RefreshQuestionAsync(request);
                return Ok(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh question");
                return StatusCode(500, "Error refreshing question");
            }
        }

        [HttpPost("download-paper")]
        public async Task<IActionResult> DownloadPaper([FromBody] DownloadPaperRequestDto request)
        {
            try
            {
                var bytes = await _repository.GenerateQuestionPaperAsync(request);
                return File(bytes, "application/pdf", "QuestionPaper.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate paper");
                return StatusCode(500, "Error generating paper");
            }
        }
    }
}
