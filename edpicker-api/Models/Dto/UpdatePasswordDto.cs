namespace edpicker_api.Models.Dto
{
    public class UpdatePasswordDto
    {   public string SchoolEmail { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
