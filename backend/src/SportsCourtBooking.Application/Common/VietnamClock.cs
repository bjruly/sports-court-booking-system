namespace SportsCourtBooking.Application.Common;

/// <summary>
/// Giờ Việt Nam (UTC+7, không có giờ mùa hè).
/// Container Docker mặc định chạy giờ UTC nên không dùng DateTime.Now / DateTime.Today
/// khi cần "hôm nay", "giờ hiện tại" theo giờ địa phương.
/// </summary>
public static class VietnamClock
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateTime Now => DateTime.UtcNow + Offset;

    public static DateOnly Today => DateOnly.FromDateTime(Now);
}
