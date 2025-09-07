using edpicker_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace edpicker_api.Controllers
{
    //[Route("api/[controller]")]
    [Route("{schoolid}/webhook/whatsapp")]// Define your webhook endpoint route
    [ApiController]
    public class WhatsAppWebhookController : ControllerBase
    {
        private readonly ILogger<WhatsAppWebhookController> _logger;
        private readonly IConfiguration _configuration;

        public WhatsAppWebhookController(ILogger<WhatsAppWebhookController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verifyToken)
        {
            var schoolid = (string)RouteData.Values["schoolid"];
            _logger.LogInformation($"Webhook Verification Request received - Mode: {mode}, Challenge: {challenge}, Verify Token: {verifyToken}");

            string expectedVerifyToken = "HelloEdPicker"; // Store your verify token in appsettings.json or environment variables

            if (mode == "subscribe" && verifyToken == expectedVerifyToken)
            {
                _logger.LogInformation("Webhook verified successfully!");
                return Ok(challenge); // Respond with the challenge token to verify
            }
            else
            {
                _logger.LogWarning("Webhook verification failed. Incorrect verify token or mode.");
                return BadRequest("Webhook verification failed. Incorrect verify token or mode.");
            }
        }

        [HttpPost]
        public IActionResult ReceiveWebhookEvent([FromBody] object webhookPayload)
        {
            var schoolid = (string)RouteData.Values["schoolid"];
            _logger.LogInformation("Webhook Event Received:");
            _logger.LogInformation(JsonConvert.SerializeObject(webhookPayload, Formatting.Indented)); // Log the entire payload for debugging

            // **Important:** Process the webhook event payload here.
            // You'll need to deserialize the 'webhookPayload' object to access the data.
            // Refer to WhatsApp Cloud API documentation for the structure of webhook events.

            try
            {
                var payload = JsonConvert.DeserializeObject<WebhookPayload>(webhookPayload.ToString());

                // Deserialize the object to a dynamic or specific class if you define one
               // dynamic payload = webhookPayload;

                // Example of accessing fields (adjust based on actual payload structure)
                if (payload.Entry != null && payload.Entry.Count > 0)
                {
                    foreach (var entry in payload.Entry)
                    {
                        if (entry.Changes != null && entry.Changes.Count > 0)
                        {
                            foreach (var change in entry.Changes)
                            {
                                if (change.Field == "messages") // Check if the update is about messages
                                {
                                    if (change.Value != null && change.Value.Messages != null)
                                    {
                                        foreach (var message in change.Value.Messages)
                                        {
                                            string messageType = message.Type;
                                            string fromNumber = change.Value.Contacts[0].WaId; // Sender's WhatsApp ID
                                            string messageText = "";

                                            if (messageType == "text")
                                            {
                                                messageText = message.Text.Body;
                                            }
                                            // Handle other message types (image, audio, etc.) as needed

                                            _logger.LogInformation($"Message Received - Type: {messageType}, From: {fromNumber}, Text: {messageText}");

                                            // **Your logic to process the message goes here**
                                            // For example, you can send a response back using WhatsApp Cloud API
                                        }
                                    }
                                }
                                // Handle other field updates (statuses, etc.) if subscribed
                            }
                        }
                    }
                }

                return Ok("EVENT_RECEIVED"); // Acknowledge receipt of the event
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing webhook event: {ex}");
                return StatusCode(500, "Error processing event"); // Indicate an error if processing fails
            }
        }
    }
    
}
