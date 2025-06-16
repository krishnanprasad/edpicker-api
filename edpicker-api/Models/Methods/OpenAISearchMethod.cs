using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace edpicker_api.Models.Methods
{
    public class OpenAISearchMethod
    {
       
            private readonly HttpClient _httpClient;

            public OpenAISearchMethod(string openAIApiKey)
            {
                // Set up an HttpClient with the necessary authorization
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", openAIApiKey);
            }

        /// <summary>
        /// Searches the specified OpenAI file for the user's query and returns a snippet indicator.
        /// Note: The /v1/search endpoint only returns a document index, not the actual text snippet.
        /// You typically store the file in lines or JSON lines so each "document" is a chunk.
        /// Then you can fetch that line from your original source if needed.
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(string text, string model = "text-embedding-ada-002")
        {
            var requestBody = new
            {
                input = text,
                model = model
            };

      
                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
            if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OpenAI search failed: {response.StatusCode} - {errorMsg}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseJson);

                // result.data is an array of { "object": "search_result", "document": int, "score": float }
                // The best match is typically result.data[0]
                if (result.data != null && result.data.Count > 0)
                {
                    int bestDocIndex = result.data[0].document;
                    double bestScore = result.data[0].score;

                    // For demonstration, we'll just return a string referencing the doc index and score.
                    // In a real scenario, you'd map bestDocIndex back to the actual line from your file
                    // so you can pass that snippet text to GPT.
                    return $"(Best match is doc index {bestDocIndex}, score={bestScore})";
                }

                return "No relevant snippet found in the file.";
            }

            /// <summary>
            /// Passes the snippet + user question to GPT (chat completion) to get a final answer.
            /// </summary>
            public async Task<string> GetAnswerFromGPTAsync(string snippet, string userQuery, string model = "gpt-3.5-turbo")
            {
                var requestData = new
                {
                    model = model,
                    messages = new[]
                    {
                    new { role = "system", content = "You are a helpful assistant for a school." },
                    new { role = "user", content = $"Context:\n{snippet}\n\nQuestion: {userQuery}" }
                }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"GPT call failed: {response.StatusCode} - {errorMsg}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseJson);

                // The answer is typically in result.choices[0].message.content
                string answer = result.choices[0].message.content;
                return answer;
            }
        
    }
}
