using System.Text;
using System.Linq;
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

                var allQuestions = new List<QuestionDto>();
                foreach (var qt in request.QuestionTypes)
                {
                    var modifiedRequest = new GenerateQuestionsRequestDto
                    {
                        Class = request.Class,
                        Subject = request.Subject,
                        Chapter = request.Chapter,
                        Topic = request.Topic,
                        QuestionTypes = request.QuestionTypes,
                        Difficulty = request.Difficulty,
                        QuestionType = qt.Type,
                        NumberOfQuestions = qt.NumberOfQuestions,
                        Section = qt.Section
                    };

                    var result = await GenerateQuestionsWithOpenAIAsync(modifiedRequest, content, openAIApiKey);

                    // Set questionType and section for each question
                    foreach (var question in result)
                    {
                        question.QuestionType = qt.Type;
                        question.Section = qt.Section;
                    }

                    allQuestions.AddRange(result);
                }

                return allQuestions;
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
                var allQuestions = new List<QuestionDto>();

                foreach (var qt in request.QuestionTypes)
                {
                    var promptBuilder = new StringBuilder();
                    promptBuilder.Append($"Create {qt.NumberOfQuestions} {qt.Type} questions for Class {request.Class} {request.Subject} Chapter {request.Chapter} on topic {request.Topic}. ");
                    if (!string.Equals(qt.Section, "any", StringComparison.OrdinalIgnoreCase))
                        promptBuilder.Append($"Section: {qt.Section}. ");
                    promptBuilder.Append($"Difficulty: {request.Difficulty}. Use only the provided textbook chapters as the source. Avoid verbatim copying; keep questions unique and syllabus-aligned. ");

                    if (qt.Type == QuestionType.MCQ)
                    {
                        promptBuilder.Append("For each question, provide four options and the correct answer as one of the options. ");
                        promptBuilder.Append("Format your response as a JSON object with a 'questions' property containing an array of objects with 'QuestionText', 'Options' (array of 4 strings), 'Answer' (the correct option text), 'QuestionType', and 'Section'. ");
                        promptBuilder.Append($"Example format: {{\"questions\": [{{\"QuestionText\": \"Sample question?\", \"Options\": [\"Option 1\", \"Option 2\", \"Option 3\", \"Option 4\"], \"Answer\": \"Option 1\", \"QuestionType\": \"{qt.Type}\", \"Section\": \"{qt.Section}\"}}]}}");
                    }
                    else if (qt.Type == QuestionType.TwoMark)
                    {
                        promptBuilder.Append("Each answer should be about two sentences long and include a brief hint. ");
                        promptBuilder.Append("Format your response as a JSON object with a 'questions' property containing an array of objects with 'QuestionText', 'Answer', 'Hint', 'QuestionType', and 'Section'. ");
                        promptBuilder.Append($"Example format: {{\"questions\": [{{\"QuestionText\": \"Sample question?\", \"Answer\": \"Sample answer\", \"Hint\": \"Sample hint\", \"QuestionType\": \"{qt.Type}\", \"Section\": \"{qt.Section}\"}}]}}");
                    }
                    else if (qt.Type == QuestionType.ThreeMark)
                    {
                        promptBuilder.Append("Each answer should be about three sentences long and include a brief hint. ");
                        promptBuilder.Append("Format your response as a JSON object with a 'questions' property containing an array of objects with 'QuestionText', 'Answer', 'Hint', 'QuestionType', and 'Section'. ");
                        promptBuilder.Append($"Example format: {{\"questions\": [{{\"QuestionText\": \"Sample question?\", \"Answer\": \"Sample answer\", \"Hint\": \"Sample hint\", \"QuestionType\": \"{qt.Type}\", \"Section\": \"{qt.Section}\"}}]}}");
                    }
                    else if (qt.Type == QuestionType.FourMark)
                    {
                        promptBuilder.Append("Each answer should be about four sentences long and include a helpful hint. ");
                        promptBuilder.Append("Format your response as a JSON object with a 'questions' property containing an array of objects with 'QuestionText', 'Answer', 'Hint', 'QuestionType', and 'Section'. ");
                        promptBuilder.Append($"Example format: {{\"questions\": [{{\"QuestionText\": \"Sample question?\", \"Answer\": \"Sample answer\", \"Hint\": \"Sample hint\", \"QuestionType\": \"{qt.Type}\", \"Section\": \"{qt.Section}\"}}]}}");
                    }
                    else
                    {
                        promptBuilder.Append("Each answer should be about five sentences long and include a helpful hint. ");
                        promptBuilder.Append("Format your response as a JSON object with a 'questions' property containing an array of objects with 'QuestionText', 'Answer', 'Hint', 'QuestionType', and 'Section'. ");
                        promptBuilder.Append($"Example format: {{\"questions\": [{{\"QuestionText\": \"Sample question?\", \"Answer\": \"Sample answer\", \"Hint\": \"Sample hint\", \"QuestionType\": \"{qt.Type}\", \"Section\": \"{qt.Section}\"}}]}}");
                    }

                    var messages = new List<ChatMessage>
                    {
                        new SystemChatMessage("You are an expert question paper generator for school students. Always respond with valid JSON format containing a 'questions' array. Include QuestionType and Section in each question object."),
                        new UserChatMessage(promptBuilder.ToString())
                    };

                    var chatCompletion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                    {
                        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                        Temperature = 0.7f
                    });

                    var responseContent = chatCompletion.Value.Content[0].Text;

                    // Parse the response
                    List<QuestionDto> questions;
                    try
                    {
                        var wrapper = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        if (wrapper?.questions != null)
                        {
                            questions = JsonConvert.DeserializeObject<List<QuestionDto>>(wrapper.questions.ToString()) ?? new List<QuestionDto>();
                        }
                        else
                        {
                            // Fallback: try direct array deserialization
                            questions = JsonConvert.DeserializeObject<List<QuestionDto>>(responseContent) ?? new List<QuestionDto>();
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "JSON parsing failed. Response: {Response}", responseContent);
                        questions = new List<QuestionDto>();
                    }

                    foreach (var q in questions)
                    {
                        if (string.IsNullOrEmpty(q.QuestionId))
                            q.QuestionId = Guid.NewGuid().ToString();

                        // Ensure questionType and section are set even if not returned by AI
                        if (q.QuestionType == null)
                            q.QuestionType = qt.Type;
                        if (string.IsNullOrEmpty(q.Section))
                            q.Section = qt.Section;
                    }

                    allQuestions.AddRange(questions);
                }

                return allQuestions;
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
                    Hint = "Sample hint",
                    QuestionType = request.QuestionType,
                    Section = request.Section
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

            // **Ensure exact count**
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
                    QuestionId = Guid.NewGuid().ToString(),
                    QuestionType = request.QuestionType,
                    Section = request.Section
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
                    case QuestionType.TwoMark:
                    case QuestionType.ThreeMark:
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

                    var sentences = paragraph.Split(SentenceDelimiters, StringSplitOptions.RemoveEmptyEntries);
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
            if (!string.Equals(request.Section, "any", StringComparison.OrdinalIgnoreCase))
                promptBuilder.AppendLine($"Section: {request.Section}.");
            promptBuilder.AppendLine($"The questions should be at \"{request.Difficulty}\" difficulty level.");

            if (request.QuestionType == QuestionType.MCQ)
            {
                promptBuilder.AppendLine("For each question, provide:");
                promptBuilder.AppendLine("- The question text");
                promptBuilder.AppendLine("- Four options");
                promptBuilder.AppendLine("- The correct answer (one of the options)");
                promptBuilder.AppendLine($"Format your response as a JSON array of objects with \"QuestionText\", \"Options\" (array of four strings), \"Answer\" (the correct option text), \"QuestionType\": \"{request.QuestionType}\", and \"Section\": \"{request.Section}\".");
                promptBuilder.AppendLine("Example:");
                promptBuilder.AppendLine("[");
                promptBuilder.AppendLine($"  {{ \"QuestionText\": \"Sample question?\", \"Options\": [\"Option 1\", \"Option 2\", \"Option 3\", \"Option 4\"], \"Answer\": \"Option 1\", \"QuestionType\": \"{request.QuestionType}\", \"Section\": \"{request.Section}\" }}");
                promptBuilder.AppendLine("]");
            }
            else
            {
                int sentences = request.QuestionType switch
                {
                    QuestionType.TwoMark => 2,
                    QuestionType.ThreeMark => 3,
                    QuestionType.FourMark => 4,
                    QuestionType.FiveMark => 5,
                    _ => 2
                };
                promptBuilder.AppendLine("For each question, provide:");
                promptBuilder.AppendLine("- The question text");
                promptBuilder.AppendLine("- A helpful hint");
                promptBuilder.AppendLine($"- An answer about {sentences} sentences long");
                promptBuilder.AppendLine($"Format your response as a JSON array of objects, each with \"QuestionText\", \"Hint\", \"Answer\", \"QuestionType\": \"{request.QuestionType}\", and \"Section\": \"{request.Section}\".");
                promptBuilder.AppendLine("Example:");
                promptBuilder.AppendLine("[");
                promptBuilder.AppendLine($"  {{ \"QuestionText\": \"Sample question 1...\", \"Hint\": \"Sample hint 1\", \"Answer\": \"Answer\", \"QuestionType\": \"{request.QuestionType}\", \"Section\": \"{request.Section}\" }},");
                promptBuilder.AppendLine($"  {{ \"QuestionText\": \"Sample question 2...\", \"Hint\": \"Sample hint 2\", \"Answer\": \"Answer\", \"QuestionType\": \"{request.QuestionType}\", \"Section\": \"{request.Section}\" }}");
                promptBuilder.AppendLine("]");
            }

            var prompt = promptBuilder.ToString();

            try
            {
                string response = await openAiHelper.GetAnswerFromGPTAsync(string.Empty, prompt);

                var questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionDto>>(response)
                               ?? new List<QuestionDto>();

                foreach (var q in questions)
                {
                    q.QuestionId = Guid.NewGuid().ToString();
                    // Ensure questionType and section are set
                    if (q.QuestionType == null)
                        q.QuestionType = request.QuestionType;
                    if (string.IsNullOrEmpty(q.Section))
                        q.Section = request.Section;
                }

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