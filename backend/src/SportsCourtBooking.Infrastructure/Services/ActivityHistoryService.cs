using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Domain.Enums;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Infrastructure.Services;

public sealed class ActivityHistoryService(AppDbContext context) : IActivityHistoryService
{
    private const int PageSize = 25;

    public async Task<ActivityHistoryResult> GetPageAsync(
        int adminId, UserRole? role, DateOnly? date, int page, CancellationToken cancellationToken = default)
    {
        if (!await context.Users.AnyAsync(
                user => user.Id == adminId && user.Role == UserRole.Admin && user.IsActive, cancellationToken))
        {
            throw new UnauthorizedAccessException("Chỉ quản trị viên được xem lịch sử thao tác.");
        }

        var query = context.ActivityRecords.AsNoTracking();
        if (role.HasValue)
        {
            query = query.Where(record => record.ActorRole == role.Value);
        }

        if (date.HasValue)
        {
            var start = VietnamClock.StartOfDayUtc(date.Value);
            query = query.Where(record => record.OccurredAtUtc >= start);
            if (date.Value < DateOnly.MaxValue)
            {
                var end = VietnamClock.StartOfDayUtc(date.Value.AddDays(1));
                query = query.Where(record => record.OccurredAtUtc < end);
            }
        }

        var total = await query.CountAsync(cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, pageCount);

        var items = await (
            from record in query.OrderByDescending(item => item.OccurredAtUtc)
                .ThenByDescending(item => item.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
            join actor in context.Users.AsNoTracking() on record.ActorId equals actor.Id into actors
            from actor in actors.DefaultIfEmpty()
            orderby record.OccurredAtUtc descending, record.Id descending
            select new ActivityHistoryItem(record.Id, record.OccurredAtUtc, record.ActorId,
                actor == null ? null : actor.FullName, record.ActorRole, record.Action, record.Target))
            .ToListAsync(cancellationToken);

        return new ActivityHistoryResult(items, page, pageCount);
    }
}
