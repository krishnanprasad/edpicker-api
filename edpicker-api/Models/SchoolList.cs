using Newtonsoft.Json;

namespace edpicker_api.Models


{
    public class SchoolList
    {
        [JsonProperty("principlename")]
        public string PrincipalName { get; set; }

        [JsonProperty("totalstudentstrength")]
        public int TotalStudentStrength { get; set; }

        [JsonProperty("totalteacherstrength")]
        public int TotalTeacherStrength { get; set; }

        [JsonProperty("totalbus")]
        public int TotalBus { get; set; }
    }

    public class CommunicationDetails
    {
        [JsonProperty("primarycontact")]
        public string PrimaryContact { get; set; }

        [JsonProperty("secondarycontact")]
        public string SecondaryContact { get; set; }

        [JsonProperty("primaryemail")]
        public string PrimaryEmail { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }
    }

    public class SocialMediaDetails
    {
        [JsonProperty("facebook")]
        public string Facebook { get; set; }

        [JsonProperty("instagram")]
        public string Instagram { get; set; }

        [JsonProperty("youtube")]
        public string Youtube { get; set; }

        [JsonProperty("twitter")]
        public string Twitter { get; set; }
    }

    public class School
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("schooltype")]
        public int SchoolType { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("board")]
        public string Board { get; set; }

        [JsonProperty("schoolname")]
        public string SchoolName { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("fees")]
        public decimal Fees { get; set; }

        [JsonProperty("feesperiod")]
        public int FeesPeriod { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("startedon")]
        public DateTime StartedOn { get; set; }

        [JsonProperty("details")]
        public SchoolList Details { get; set; }

        [JsonProperty("communication")]
        public CommunicationDetails Communication { get; set; }

        [JsonProperty("socialmedia")]
        public SocialMediaDetails SocialMedia { get; set; }

        [JsonProperty("photos")]
        public string Photos { get; set; }

        [JsonProperty("nextadmissiondate")]
        public string NextAdmissionDate { get; set; }
    }
}
