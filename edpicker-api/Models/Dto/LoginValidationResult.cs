namespace edpicker_api.Models.Dto
{
    public class LoginValidationResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; }
        public int? UserId { get; set; }
    }
}
