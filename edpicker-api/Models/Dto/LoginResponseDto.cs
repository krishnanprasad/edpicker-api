using System.Text.Json.Serialization;

namespace edpicker_api.Models.Dto
{
    public class LoginResponseDto
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("schoolId")]
        public int SchoolId { get; set; }

        [JsonPropertyName("schoolName")]
        public string SchoolName { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime ExpiryDate { get; set; }
    }
}
