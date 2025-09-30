using edpicker_api.Models.Celebration.Enums;

namespace edpicker_api.Models.Celebration.Dtos;

public record TeacherEventDto(
    int TeacherId,
    string Name,
    CelebrationEventType EventType,
    string Phone,
    CelebrationDeliveryStatus Status,
    int Attempt,
    DateTimeOffset? SentAt,
    string? FailureReason
);

public record StudentSummaryDto(
    int StudentId,
    string Name,
    string Phone
);

public record DashboardAlertDto(
    string Message,
    string Severity
);

public class TodayDashboardResponse
{
    public DateOnly Date { get; init; }

    public string? SchoolName { get; init; }

    public IReadOnlyCollection<TeacherEventDto> Teachers { get; init; } = Array.Empty<TeacherEventDto>();

    public int StudentsCount { get; init; }

    public IReadOnlyCollection<StudentSummaryDto> Students { get; init; } = Array.Empty<StudentSummaryDto>();

    public int TotalEvents { get; init; }

    public int TotalSent { get; init; }

    public int TotalFailed { get; init; }

    public decimal DeliverySuccessRate { get; init; }

    public IReadOnlyCollection<DashboardAlertDto> Alerts { get; init; } = Array.Empty<DashboardAlertDto>();
}
