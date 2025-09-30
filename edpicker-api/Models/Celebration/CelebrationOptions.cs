namespace edpicker_api.Models.Celebration;

public class CelebrationOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string SenderId { get; set; } = "SCHLBDY";

    public string? PeId { get; set; }
        = null;

    public string? BirthdayTemplateId { get; set; }
        = null;

    public string? AnniversaryTemplateId { get; set; }
        = null;

    public string BaseUrl { get; set; } = "https://api.msg91.com/";

    public string TimeZoneId { get; set; } = "Asia/Kolkata";

    public int NotificationHour { get; set; } = 9;

    public int NotificationMinute { get; set; } = 0;

    public int SendRetryLimit { get; set; } = 3;

    public int LogRetentionDays { get; set; } = 90;
}
