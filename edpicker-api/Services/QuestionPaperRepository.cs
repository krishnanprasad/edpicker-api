using System.Text;
using edpicker_api.Models.Dto;
using edpicker_api.Models.Enum;
using edpicker_api.Models.Methods;
using edpicker_api.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using OpenAI;
using OpenAI.Chat;
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
            { ("12", "Physics", "ELECTRIC CHARGES AND FIELDS"), ("vs_68a5899b87a48191a97a4a0d2919eca0", "file-8ZvmWMYWF4x5ujvKLJ7fi5") }
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
                string? openAIApiKeyTest = _configuration["OpenAIKey"];
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

                var chatClient = _openAIClient.GetChatClient("gpt-4o-mini");

                var promptBuilder = new StringBuilder();
                promptBuilder.Append($"Create {request.NumberOfQuestions} {request.QuestionType} questions for Class {request.Class} {request.Subject} Chapter {request.Chapter} on topic {request.Topic}. ");
                promptBuilder.Append($"Difficulty: {request.Difficulty}. Use only the provided textbook chapters as the source. Avoid verbatim copying; keep questions unique and syllabus-aligned. ");

                if (request.QuestionType == QuestionType.MCQ)
                {
                    promptBuilder.Append("For each question, provide four options and the correct answer as one of the options. ");
                    promptBuilder.Append("Format your response as a JSON array with objects containing 'QuestionText', 'Options' (array of 4 strings), and 'Answer' (the correct option text). ");
                }
                else if (request.QuestionType == QuestionType.Short)
                {
                    promptBuilder.Append("Each answer should be about two sentences long and include a brief hint. ");
                    promptBuilder.Append("Format your response as a JSON array with objects containing 'QuestionText', 'Answer', and 'Hint'. ");
                }
                else
                {
                    promptBuilder.Append("Each answer should be about five sentences long and include a helpful hint. ");
                    promptBuilder.Append("Format your response as a JSON array with objects containing 'QuestionText', 'Answer', and 'Hint'. ");
                }

                promptBuilder.Append("Example format: [{\"QuestionText\": \"Sample question?\", \"Answer\": \"Sample answer\", \"Hint\": \"Sample hint\"}]");

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are an expert question paper generator for school students. Always respond with valid JSON format."),
                    new UserChatMessage(promptBuilder.ToString())
                };

                var chatCompletion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                    Temperature = 0.7f
                });

                // Replace the problematic parsing section with this improved version
                var responseContent = chatCompletion.Value.Content[0].Text;

                List<QuestionDto> questions;
                try
                {
                    // First, try to parse as a wrapper object with "questions" property
                    var wrapper = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    if (wrapper?.questions != null)
                    {
                        // The response has a "questions" property - extract it
                        questions = JsonConvert.DeserializeObject<List<QuestionDto>>(wrapper.questions.ToString()) ?? new List<QuestionDto>();
                    }
                    else if (wrapper?.items != null)
                    {
                        // Fallback: try "items" property
                        questions = JsonConvert.DeserializeObject<List<QuestionDto>>(wrapper.items.ToString()) ?? new List<QuestionDto>();
                    }
                    else
                    {
                        // Fallback: try to parse the entire response as an array
                        questions = JsonConvert.DeserializeObject<List<QuestionDto>>(responseContent) ?? new List<QuestionDto>();
                    }
                }
                catch (JsonException ex)
                {
                    // If JSON parsing fails, log the raw response and create empty list
                    _logger.LogWarning(ex, "Failed to parse OpenAI response. Raw content: {ResponseContent}", responseContent);
                    questions = new List<QuestionDto>();
                }

                foreach (var q in questions)
                {
                    if (string.IsNullOrEmpty(q.QuestionId))
                        q.QuestionId = Guid.NewGuid().ToString();
                }

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions via OpenAI chat API");
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