namespace edpicker_api.Models.Celebration.Dtos;

public class UploadProfilesResult
{
    public int Imported { get; init; }

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
