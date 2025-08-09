using System.Text;
using edpicker_api.Models.Dto;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace edpicker_api.Services
{
    public class QuestionPaperRepository : IQuestionPaperRepository
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<QuestionPaperRepository> _logger;

        public QuestionPaperRepository(IWebHostEnvironment env, ILogger<QuestionPaperRepository> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<IEnumerable<QuestionDto>> GenerateQuestionsAsync(GenerateQuestionsRequestDto request)
        {
            var questions = new List<QuestionDto>();
            try
            {
                string pdfPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "PDFs", request.Subject, $"{request.Topic}.pdf");
                string content = ReadPdfContent(pdfPath);
                // TODO: Use OpenAI to create questions from content
                for (int i = 0; i < request.NumberOfQuestions; i++)
                {
                    questions.Add(new QuestionDto
                    {
                        QuestionId = Guid.NewGuid().ToString(),
                        QuestionText = $"Sample question {i + 1} for {request.Topic}",
                        Hint = "Sample hint"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions");
                throw;
            }
            return await Task.FromResult(questions);
        }

        public async Task<QuestionDto> RefreshQuestionAsync(RefreshQuestionRequestDto request)
        {
            try
            {
                // Generate a new sample question
                var question = new QuestionDto
                {
                    QuestionId = Guid.NewGuid().ToString(),
                    QuestionText = $"Refreshed question for {request.Topic}",
                    Hint = "Sample hint"
                };
                return await Task.FromResult(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing question");
                throw;
            }
        }

        public async Task<byte[]> GenerateQuestionPaperAsync(DownloadPaperRequestDto request)
        {
            try
            {
                var sb = new StringBuilder();
                int count = 1;
                foreach (var q in request.Questions)
                {
                    sb.AppendLine($"{count}. {q.QuestionText}");
                    sb.AppendLine();
                    count++;
                }
                return await Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating paper");
                throw;
            }
        }

        private string ReadPdfContent(string path)
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("PDF not found at {Path}", path);
                return string.Empty;
            }
            var sb = new StringBuilder();
            using (PdfDocument pdf = PdfDocument.Open(path))
            {
                foreach (Page page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }
    }
}
