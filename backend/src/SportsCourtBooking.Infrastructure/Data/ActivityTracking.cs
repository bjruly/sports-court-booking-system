using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Infrastructure.Data;

internal static class ActivityTracking
{
    // Ghi cùng lần SaveChanges hoặc cùng giao dịch với thao tác nghiệp vụ.
    internal static ActivityRecord RecordActivity(
        this AppDbContext context, UserRole actorRole, int actorId, string action, string target)
    {
        var record = new ActivityRecord
        {
            ActorId = actorId,
            ActorRole = actorRole,
            Action = action,
            Target = target,
            OccurredAtUtc = DateTime.UtcNow
        };

        context.ActivityRecords.Add(record);
        return record;
    }

    internal static void RecordAdminActivity(
        this AppDbContext context, int? adminId, string action, string target)
    {
        if (adminId.HasValue)
        {
            context.RecordActivity(UserRole.Admin, adminId.Value, action, target);
        }
    }
}
