using System.Text.Json.Serialization;

namespace edpicker_api.Models.Dto
{
    public class LoginRequestDto
    {
        [JsonPropertyName("schoolCode")]
        public string SchoolCode { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
