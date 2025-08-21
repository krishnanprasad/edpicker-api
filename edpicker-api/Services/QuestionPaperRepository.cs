using System.Text;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Enum;
using edpicker_api.Models.Methods;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Responses;
using Newtonsoft.Json;

namespace edpicker_api.Services
{
    public class QuestionPaperRepository : IQuestionPaperRepository
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<QuestionPaperRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly OpenAIClient _openAIClient;
        private static readonly string[] SentenceDelimiters = { ".", "!", "?" };

        // Temporary in-memory mapping; replace with SQL-backed storage later
        private readonly Dictionary<(string Class, string Subject, string Chapter), (string VectorStoreId, string FileId)> _resourceMap = new()
        {
            { ("9", "Science", "Heat"), ("vs_school42_cls9_science_2025", "file_heat_001") }
        };

        public QuestionPaperRepository(IWebHostEnvironment env, ILogger<QuestionPaperRepository> logger, IConfiguration configuration, OpenAIClient openAIClient)
        {
            _env = env;
            _logger = logger;
            _configuration = configuration;
            _openAIClient = openAIClient;
        }

        public async Task<IEnumerable<QuestionDto>> GenerateQuestionsAsync(GenerateQuestionsRequestDto request)
        {
            try
            {
                string contentDir = Path.Combine(_env.ContentRootPath, "Content", "9thScience");
                var pdfFiles = Directory.GetFiles(contentDir, "*.pdf");

                // Combine content from all PDFs
                var sb = new StringBuilder();
                foreach (var pdfPath in pdfFiles)
                {
                    sb.AppendLine(ReadPdfContent(pdfPath));
                }
                string content = sb.ToString();
                string openAIApiKeyTest = _configuration["OpenAIKey"];
                // Get your OpenAI API key from configuration/environment
                string openAIApiKey = Environment.GetEnvironmentVariable("OpenAIKey") ?? "";

                var questions = await GenerateQuestionsWithOpenAIAsync(request, content, openAIApiKey);

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions");
                throw;
            }
        }

        public async Task<IEnumerable<QuestionDto>> GenerateQuestionsWithResponsesAsync(GenerateQuestionsRequestDto request)
        {
            try
            {
                if (!_resourceMap.TryGetValue((request.Class, request.Subject, request.Chapter), out var resources))
                    throw new ArgumentException($"No resources mapped for class {request.Class}, subject {request.Subject}, chapter {request.Chapter}");

                var (vectorStoreId, fileId) = resources;

                var responsesClient = _openAIClient.GetResponsesClient();

                var promptBuilder = new StringBuilder();
                promptBuilder.Append($"Create {request.NumberOfQuestions} {request.QuestionType} questions for Class {request.Class} {request.Subject} Chapter {request.Chapter} on topic {request.Topic}. ");
                promptBuilder.Append($"Difficulty: {request.Difficulty}. Use only the provided textbook chapters as the source. Avoid verbatim copying; keep questions unique and syllabus-aligned. ");

                if (request.QuestionType == QuestionType.MCQ)
                {
                    promptBuilder.Append("For each question, provide four options and the index of the correct option.");
                }
                else if (request.QuestionType == QuestionType.Short)
                {
                    promptBuilder.Append("Each answer should be about two sentences long and include a brief hint.");
                }
                else
                {
                    promptBuilder.Append("Each answer should be about five sentences long and include a helpful hint.");
                }

                var schema = new
                {
                    type = "object",
                    properties = new
                    {
                        items = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    QuestionText = new { type = "string" },
                                    Options = new
                                    {
                                        type = "array",
                                        items = new { type = "string" },
                                        minItems = 4,
                                        maxItems = 4
                                    },
                                    Answer = new { type = "string" },
                                    Hint = new { type = "string" }
                                },
                                required = request.QuestionType == QuestionType.MCQ
                                    ? new[] { "QuestionText", "Options", "Answer" }
                                    : new[] { "QuestionText", "Answer", "Hint" }
                            }
                        }
                    },
                    required = new[] { "items" }
                };

                var response = await responsesClient.CreateResponseAsync(new ResponseCreationOptions
                {
                    Model = "gpt-4o-mini",
                    Input = promptBuilder.ToString(),
                    Tools = { new FileSearchToolDefinition() },
                    ToolResources = new()
                    {
                        FileSearch = new()
                        {
                            VectorStoreIds = { vectorStoreId },
                            FileIds = { fileId }
                        }
                    },
                    ResponseFormat = ResponseFormat.CreateJsonSchema(
                        name: "question_set",
                        schema: schema,
                        strict: true)
                });

                var json = response.Output[0].Content[0].Text;
                var questions = JsonConvert.DeserializeObject<List<QuestionDto>>(json) ?? new List<QuestionDto>();

                foreach (var q in questions)
                {
                    if (string.IsNullOrEmpty(q.QuestionId))
                        q.QuestionId = Guid.NewGuid().ToString();
                }

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions via responses API");
                throw;
            }
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
        private async Task<List<QuestionDto>> GenerateQuestionsWithOpenAIAsync(GenerateQuestionsRequestDto request, string content, string openAIApiKey)
        {
            var openAiHelper = new OpenAISearchMethod(openAIApiKey);
            var allQuestions = new List<QuestionDto>();

            // Split content into chunks to stay within token limits
            var contentChunks = ChunkContent(content, maxChunkSize: 8000);

            // If no chunks available, create fallback questions
            if (contentChunks.Count == 0)
            {
                return await GenerateFallbackQuestions(request);
            }

            int questionsPerChunk = Math.Max(1, request.NumberOfQuestions / contentChunks.Count);
            int remainingQuestions = request.NumberOfQuestions;

            // First pass: Generate questions from chunks
            foreach (var chunk in contentChunks)
            {
                if (remainingQuestions <= 0) break;

                // Calculate questions for this chunk
                int questionsForThisChunk = Math.Min(questionsPerChunk, remainingQuestions);
                if (chunk == contentChunks.Last()) // Last chunk gets any remaining questions
                {
                    questionsForThisChunk = remainingQuestions;
                }

                var chunkQuestions = await GenerateQuestionsFromChunk(
                    openAiHelper, request, chunk, questionsForThisChunk);

                allQuestions.AddRange(chunkQuestions);
                remainingQuestions -= questionsForThisChunk;
            }

            // **NEW LOGIC: Ensure exact count**
            if (allQuestions.Count > request.NumberOfQuestions)
            {
                // Too many questions - randomly select the exact number
                var random = new Random();
                allQuestions = allQuestions
                    .OrderBy(x => random.Next())
                    .Take(request.NumberOfQuestions)
                    .ToList();
            }
            else if (allQuestions.Count < request.NumberOfQuestions)
            {
                // Too few questions - generate more from the best chunks
                int questionsNeeded = request.NumberOfQuestions - allQuestions.Count;

                // Use the largest chunks to generate additional questions
                var bestChunks = contentChunks
                    .OrderByDescending(c => c.Length)
                    .Take(Math.Min(3, contentChunks.Count)) // Use top 3 largest chunks
                    .ToList();

                foreach (var chunk in bestChunks)
                {
                    if (questionsNeeded <= 0) break;

                    var additionalQuestions = await GenerateQuestionsFromChunk(
                        openAiHelper, request, chunk, questionsNeeded);

                    // Filter out duplicates based on question text similarity
                    var newQuestions = additionalQuestions
                        .Where(newQ => !allQuestions.Any(existingQ =>
                            AreSimilarQuestions(existingQ.QuestionText, newQ.QuestionText)))
                        .Take(questionsNeeded)
                        .ToList();

                    allQuestions.AddRange(newQuestions);
                    questionsNeeded -= newQuestions.Count;
                }

                // If still not enough, generate simple fallback questions
                if (allQuestions.Count < request.NumberOfQuestions)
                {
                    int stillNeeded = request.NumberOfQuestions - allQuestions.Count;
                    var fallbackQuestions = await GenerateFallbackQuestions(request, stillNeeded);
                    allQuestions.AddRange(fallbackQuestions);
                }

                // Final trim if we somehow got too many
                if (allQuestions.Count > request.NumberOfQuestions)
                {
                    var random = new Random();
                    allQuestions = allQuestions
                        .OrderBy(x => random.Next())
                        .Take(request.NumberOfQuestions)
                        .ToList();
                }
            }

            return allQuestions;
        }
        private bool AreSimilarQuestions(string question1, string question2)
        {
            if (string.IsNullOrWhiteSpace(question1) || string.IsNullOrWhiteSpace(question2))
                return false;

            // Simple similarity check - you can make this more sophisticated
            var words1 = question1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = question2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var commonWords = words1.Intersect(words2).Count();
            var totalWords = Math.Min(words1.Length, words2.Length);

            // Consider similar if more than 60% words are common
            return totalWords > 0 && (double)commonWords / totalWords > 0.6;
        }

        // Helper method to generate fallback questions when content is insufficient
        private async Task<List<QuestionDto>> GenerateFallbackQuestions(GenerateQuestionsRequestDto request, int count = -1)
        {
            var questionsToGenerate = count == -1 ? request.NumberOfQuestions : count;
            var fallbackQuestions = new List<QuestionDto>();

            for (int i = 0; i < questionsToGenerate; i++)
            {
                var question = new QuestionDto
                {
                    QuestionId = Guid.NewGuid().ToString()
                };

                switch (request.QuestionType)
                {
                    case QuestionType.MCQ:
                        question.QuestionText = $"Which option best describes {request.Topic}? (Question {i + 1})";
                        question.Hint = $"Review the concepts of {request.Topic}";
                        question.Options = new List<string>
                        {
                            $"Option A about {request.Topic}",
                            $"Option B about {request.Topic}",
                            $"Option C about {request.Topic}",
                            $"Option D about {request.Topic}"
                        };
                        question.Answer = question.Options[0];
                        break;
                    case QuestionType.Short:
                        question.QuestionText = $"What are the key concepts in {request.Topic} related to {request.Subject}? (Question {i + 1})";
                        question.Hint = $"Think about the fundamental principles and applications in {request.Topic}";
                        question.Answer = $"Key concepts in {request.Topic} include fundamental principles and their practical applications in {request.Subject}.";
                        break;
                    default:
                        question.QuestionText = $"Explain the key concepts in {request.Topic} related to {request.Subject}. (Question {i + 1})";
                        question.Hint = $"Provide a detailed explanation covering various aspects of {request.Topic}";
                        question.Answer = $"The key concepts in {request.Topic} encompass a comprehensive understanding of fundamental principles, theoretical frameworks, practical applications, and real-world implications. These concepts form the foundation for advanced study in {request.Subject} and provide essential knowledge for understanding complex relationships and processes within this field of study.";
                        break;
                }

                fallbackQuestions.Add(question);
            }

            return await Task.FromResult(fallbackQuestions);
        }
        private List<string> ChunkContent(string content, int maxChunkSize = 8000)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(content))
                return chunks;

            // Split by paragraphs first to maintain context
            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var currentChunk = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                // If adding this paragraph would exceed chunk size, save current chunk
                if (currentChunk.Length + paragraph.Length > maxChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // If single paragraph is too large, split it by sentences
                if (paragraph.Length > maxChunkSize)
                {
                    
                    var sentences = paragraph.Split(SentenceDelimiters, StringSplitOptions.RemoveEmptyEntries); // Fix for CS1012
                    foreach (var sentence in sentences)
                    {
                        if (currentChunk.Length + sentence.Length + 2 > maxChunkSize && currentChunk.Length > 0)
                        {
                            chunks.Add(currentChunk.ToString().Trim());
                            currentChunk.Clear();
                        }
                        currentChunk.AppendLine(sentence.Trim() + ".");
                    }
                }
                else
                {
                    currentChunk.AppendLine(paragraph);
                }
            }

            // Add the last chunk if it has content
            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
            }

            return chunks;
        }

        private async Task<List<QuestionDto>> GenerateQuestionsFromChunk(
            OpenAISearchMethod openAiHelper,
            GenerateQuestionsRequestDto request,
            string contentChunk,
            int numberOfQuestions)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are an expert question paper generator for school students.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Based on the following content:");
            promptBuilder.AppendLine("---");
            promptBuilder.AppendLine(contentChunk);
            promptBuilder.AppendLine("---");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"Generate {numberOfQuestions} {request.QuestionType} questions for the subject \"{request.Subject}\" on the topic \"{request.Topic}\".");
            promptBuilder.AppendLine($"The questions should be at \"{request.Difficulty}\" difficulty level.");

            if (request.QuestionType == QuestionType.MCQ)
            {
                promptBuilder.AppendLine("For each question, provide:");
                promptBuilder.AppendLine("- The question text");
                promptBuilder.AppendLine("- Four options");
                promptBuilder.AppendLine("- The correct answer (one of the options)");
                promptBuilder.AppendLine("Format your response as a JSON array of objects with \"QuestionText\", \"Options\" (array of four strings), and \"Answer\" (the correct option text).");
                promptBuilder.AppendLine("Example:");
                promptBuilder.AppendLine("[");
                promptBuilder.AppendLine("  { \"QuestionText\": \"Sample question?\", \"Options\": [\"Option 1\", \"Option 2\", \"Option 3\", \"Option 4\"], \"Answer\": \"Option 1\" }");
                promptBuilder.AppendLine("]");
            }
            else
            {
                promptBuilder.AppendLine("For each question, provide:");
                promptBuilder.AppendLine("- The question text");
                promptBuilder.AppendLine("- A helpful hint");
                promptBuilder.AppendLine("- An answer based on the question type (2 lines for short answers, 5 lines for long answers)");
                promptBuilder.AppendLine("Format your response as a JSON array of objects, each with \"QuestionText\", \"Hint\", and \"Answer\" properties.");
                promptBuilder.AppendLine("Example:");
                promptBuilder.AppendLine("[");
                promptBuilder.AppendLine("  { \"QuestionText\": \"Sample question 1...\", \"Hint\": \"Sample hint 1\", \"Answer\": \"Answer\" },");
                promptBuilder.AppendLine("  { \"QuestionText\": \"Sample question 2...\", \"Hint\": \"Sample hint 2\", \"Answer\": \"Answer\" }");
                promptBuilder.AppendLine("]");
            }

            var prompt = promptBuilder.ToString();

            try
            {
                string response = await openAiHelper.GetAnswerFromGPTAsync(string.Empty, prompt);

                var questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionDto>>(response)
                               ?? new List<QuestionDto>();

                foreach (var q in questions)
                    q.QuestionId = Guid.NewGuid().ToString();

                return questions;
            }
            catch (Newtonsoft.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize questions from OpenAI response");
                return new List<QuestionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate questions from chunk, returning empty list");
                return new List<QuestionDto>();
            }
        }
    }

}
