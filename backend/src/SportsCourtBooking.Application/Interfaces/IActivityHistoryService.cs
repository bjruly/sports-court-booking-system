using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Application.Interfaces;

public sealed record ActivityHistoryItem(
    long Id,
    DateTime OccurredAtUtc,
    int ActorId,
    string? ActorName,
    UserRole ActorRole,
    string Action,
    string Target);

public sealed record ActivityHistoryResult(
    IReadOnlyList<ActivityHistoryItem> Items,
    int Page,
    int PageCount);

public interface IActivityHistoryService
{
    Task<ActivityHistoryResult> GetPageAsync(
        int adminId, UserRole? role, DateOnly? date, int page, CancellationToken cancellationToken = default);
}
