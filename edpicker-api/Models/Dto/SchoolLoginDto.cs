using System.Text.Json.Serialization;

namespace edpicker_api.Models.Dto
{
    public class SchoolLoginDto
    {
        [JsonPropertyName("email")]
        public string SchoolEmail { get; set; }
        [JsonPropertyName("password")]
        public string SchoolPassword { get; set; }
    }
}
