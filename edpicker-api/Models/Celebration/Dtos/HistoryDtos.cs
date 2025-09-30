using edpicker_api.Models.Celebration.Enums;

namespace edpicker_api.Models.Celebration.Dtos;

public record HistoryLogDto(
    long LogId,
    CelebrationRecipientType RecipientType,
    string RecipientName,
    CelebrationEventType EventType,
    string Phone,
    CelebrationDeliveryStatus Status,
    int Attempt,
    DateTimeOffset SentAt,
    string Message,
    string? FailureReason
);

public class HistoryResponse
{
    public DateOnly Date { get; init; }

    public IReadOnlyCollection<HistoryLogDto> Logs { get; init; } = Array.Empty<HistoryLogDto>();
}
