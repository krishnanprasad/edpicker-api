using Microsoft.AspNetCore.Mvc;
using edpicker_api.Models;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;
using System.ClientModel;
using CsvHelper.Configuration.Attributes;

namespace edpicker_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        // Hardcoded parent -> student mapping
        private static readonly Dictionary<string, string> ParentToStudentMap = new()
        {
            { "+10001112222", "student_abc" },
            { "+911234567890", "student_xyz" }
        };

        // Hardcoded student -> local file path
        // (In reality, store your knowledge base in Cosmos or Azure Storage)
        private static readonly Dictionary<string, string> StudentToFileMap = new()
        {
            { "student_abc", "Data/StudentFeedback11022026.txt" },
             { "student_xyz", "Data/StudentFeedback11022026.txt" },
        };

        private readonly OpenAIClient _openAIClient;
        private readonly OpenAIFileClient _fileClient;

#pragma warning disable OPENAI001
        // The following types are for evaluation purposes only 
        // and may change or be removed in future updates.
        private readonly AssistantClient _assistantClient;
#pragma warning restore OPENAI001

        public StudentController(OpenAIClient openAIClient)
        {
            // Using the OpenAIClient to get subclients
            // (Ensure you have your env variable OPENAI_API_KEY or handle config differently)
            _openAIClient = openAIClient;
            _fileClient = _openAIClient.GetOpenAIFileClient();

#pragma warning disable OPENAI001
            _assistantClient = _openAIClient.GetAssistantClient();
#pragma warning restore OPENAI001
        }

        [HttpPost("ask-performance")]
        public async Task<IActionResult> AskPerformance([FromBody] ParentPerformanceRequest request)
        {
            // 1) Validate parent phone number
            if (!ParentToStudentMap.TryGetValue(request.ParentPhoneNumber, out string studentId))
            {
                return BadRequest("Unknown parent phone number or not mapped to a student.");
            }

            // 2) Find the local knowledge-base file for this student
            if (!StudentToFileMap.TryGetValue(studentId, out string localFilePath))
            {
                return NotFound("No performance file found for this student.");
            }

            // 3) Check file existence
            if (!System.IO.File.Exists(localFilePath))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"File not found: {localFilePath}");
            }

            // 4) Upload the file so the assistant can do retrieval
            OpenAIFile performanceFile;
            try
            {
                using var fileStream = System.IO.File.OpenRead(localFilePath);

                performanceFile = await _fileClient.UploadFileAsync(
                    fileStream,
                    System.IO.Path.GetFileName(localFilePath),
                    FileUploadPurpose.Assistants
                );
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error uploading file to OpenAI: {ex.Message}");
            }

#pragma warning disable OPENAI001
            // 5) Create an Assistant with instructions about "student performance"
            //    We'll add a FileSearch tool that references this newly uploaded file
            var assistantOptions = new AssistantCreationOptions
            {
                Name = "Student Performance Assistant",
                Instructions =
                    "You are an assistant that looks up student performance data and provides concise explanations.\n" +
                    "When asked a question, retrieve the relevant data from the file, then respond helpfully.",

                Tools =
                {
                    new FileSearchToolDefinition()
                    // You could also add CodeInterpreterToolDefinition if needed
                },
                ToolResources = new()
                {
                    FileSearch = new()
                    {
                        // Build a new vector store from the uploaded file, 
                        // so the assistant can do retrieval-augmented generation
                        NewVectorStores =
                        {
                            new VectorStoreCreationHelper(new[] { performanceFile.Id })
                        }
                    }
                }
            };
#pragma warning restore OPENAI001

            // 6) Choose your model: "gpt-3.5-turbo", "gpt-4", or "gpt-4-32k" if available
#pragma warning disable OPENAI001
            Assistant assistant;
#pragma warning restore OPENAI001

            try
            {
#pragma warning disable OPENAI001
                assistant = await _assistantClient.CreateAssistantAsync("gpt-3.5-turbo", assistantOptions);
#pragma warning restore OPENAI001
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error creating Assistant: {ex.Message}");
            }

            // 7) Create the Thread with the parent's question
#pragma warning disable OPENAI001
            var threadOptions = new ThreadCreationOptions
            {
                // The parent's question is the initial user message in the conversation
                InitialMessages =
                {
                    request.Question
                }
            };
#pragma warning restore OPENAI001

#pragma warning disable OPENAI001
            ThreadRun threadRun;
#pragma warning restore OPENAI001

            try
            {
#pragma warning disable OPENAI001
                threadRun = await _assistantClient.CreateThreadAndRunAsync(assistant.Id, threadOptions);
#pragma warning restore OPENAI001
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error running Assistant thread: {ex.Message}");
            }

            // 8) Poll until the thread run is done
            while (!threadRun.Status.IsTerminal)
            {
                await Task.Delay(1000);
#pragma warning disable OPENAI001
                threadRun = _assistantClient.GetRun(threadRun.ThreadId, threadRun.Id);
#pragma warning restore OPENAI001
            }

            // 9) Retrieve messages to find the assistant's response
#pragma warning disable OPENAI001
            var messages = _assistantClient.GetMessagesAsync(
                threadRun.ThreadId,
                new MessageCollectionOptions { Order = MessageCollectionOrder.Ascending }
            );
#pragma warning restore OPENAI001

            string assistantReply = null;

#pragma warning disable OPENAI001
            await foreach (ThreadMessage msg in messages)
            {
                // The assistant's final answer typically has Role = Assistant
                if (msg.Role == MessageRole.Assistant)
                {
                    // msg.Content may contain multiple text/image segments
                    // We'll just concatenate all text
                    foreach (var contentItem in msg.Content)
                    {
                        if (!string.IsNullOrEmpty(contentItem.Text))
                        {
                            assistantReply += contentItem.Text + "\n";
                        }
                    }
                }
            }
#pragma warning restore OPENAI001

            // 10) (Optional) Clean up ephemeral resources 
            try
            {
                if (!string.IsNullOrEmpty(threadRun.ThreadId))
                {
#pragma warning disable OPENAI001
                    await _assistantClient.DeleteThreadAsync(threadRun.ThreadId);
                    await _assistantClient.DeleteAssistantAsync(assistant.Id);
#pragma warning restore OPENAI001
                }
                await _fileClient.DeleteFileAsync(performanceFile.Id);
            }
            catch
            {
                // Log or ignore cleanup errors
            }

            // 11) Return the final assistant answer
            if (string.IsNullOrWhiteSpace(assistantReply))
            {
                return Ok(new { answer = "No response from the assistant." });
            }

            return Ok(new { answer = assistantReply.Trim() });
        }
    }

    // Request model
    public class ParentPerformanceRequest
    {
        public string ParentPhoneNumber { get; set; }
        public string Question { get; set; }
    }

    // Example model for CSV parsing (not actively used here, 
    // but could be if you want to parse the CSV in .NET instead of letting AI handle it)
    public class StudentPerformanceRecord
    {
        [Name("studentid")]
        public string StudentId { get; set; }

        [Name("studentname")]
        public string StudentName { get; set; }

        [Name("class")]
        public string ClassGrade { get; set; }

        [Name("section")]
        public string Section { get; set; }

        [Name("mathremark")]
        public string MathRemark { get; set; }

        [Name("scienceremark")]
        public string ScienceRemark { get; set; }

        [Name("englishremark")]
        public string EnglishRemark { get; set; }

        [Name("languageremark")]
        public string LanguageRemark { get; set; }

        [Name("socialremark")]
        public string SocialRemark { get; set; }

        [Name("teacherfeedback")]
        public string TeacherFeedback { get; set; }
    }
}
