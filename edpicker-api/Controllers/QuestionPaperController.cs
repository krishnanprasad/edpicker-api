using edpicker_api.Models.Dto;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace edpicker_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionPaperController : ControllerBase
    {
        private readonly IQuestionPaperRepository _repository;
        private readonly ILogger<QuestionPaperController> _logger;
        private readonly EdPickerQuestionPaperDbContext _context; // Add this
        public QuestionPaperController(IQuestionPaperRepository repository, ILogger<QuestionPaperController> logger, EdPickerQuestionPaperDbContext context) // Add context
        {
            _repository = repository;
            _logger = logger;
            _context = context; // Assign context
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

        [HttpPost("generate-questions-v2")]
        public async Task<IActionResult> GenerateQuestionsV2([FromBody] GenerateQuestionsRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var questions = await _repository.GenerateQuestionsWithResponsesAsync(request);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate questions (v2)");
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
        [HttpGet("dummy-test")]
        public async Task<IActionResult> DummyTest()
        {
            return Ok("API is working fine!");
        }
        [HttpGet("school-classes/{schoolId}")]
        public async Task<IActionResult> GetSchoolClasses(int schoolId)
        {
            try
            {
                var classes = await _repository.GetSchoolClassesAsync(schoolId);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve school classes for SchoolId: {SchoolId}", schoolId);
                return StatusCode(500, "Error retrieving school classes");
            }
        }
        [HttpGet("school-subjects/{schoolId}/{classId}")]
        public async Task<IActionResult> GetSchoolSubjects(int schoolId, int classId)
        {
            try
            {
                var subjects = await _repository.GetSchoolSubjectsAsync(schoolId, classId);
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve school subjects for SchoolId: {SchoolId} and ClassId: {ClassId}", schoolId, classId);
                return StatusCode(500, new { Message = "Error retrieving school subjects", Error = ex.Message });
            }
        }
        [HttpGet("topics/{schoolId}/{subjectId}/{chapterId}")]
        public async Task<IActionResult> GetTopicsBySubjectForSchool(int schoolId, int subjectId, int chapterId)
        {
            try
            {
                var topics = await _repository.GetTopicsBySubjectForSchoolAsync(schoolId, subjectId, chapterId);
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve topics for SchoolId: {SchoolId} and SubjectId: {SubjectId}", schoolId, subjectId);
                return StatusCode(500, new { Message = "Error retrieving topics", Error = ex.Message });
            }
        }
        [HttpGet("subject-chapters/{subjectId}")]
        public async Task<IActionResult> GetSubjectChapters(int subjectId)
        {
            try
            {
                var chapters = await _repository.GetSubjectChaptersBySubjectAsync(subjectId);
                return Ok(chapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve chapters for SubjectId: {SubjectId}", subjectId);
                return StatusCode(500, new { Message = "Error retrieving chapters", Error = ex.Message });
            }
        }

    }
}
