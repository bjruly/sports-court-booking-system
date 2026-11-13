namespace SportsCourtBooking.Application.Rules;

/// <summary>
/// Quy tắc đặt sân dùng chung cho giao diện (hiển thị) và BookingService (lưu dữ liệu),
/// để giá/giờ hiển thị luôn khớp với giá/giờ lưu trong database.
/// </summary>
public static class BookingRules
{
    /// <summary>Giờ mở cửa (khung đầu tiên bắt đầu 06:00).</summary>
    public const int OpeningHour = 6;

    /// <summary>Giờ đóng cửa (khung cuối kết thúc 22:00, tức bắt đầu 21:00).</summary>
    public const int ClosingHour = 22;

    /// <summary>Một booking gồm tối đa 4 khung 1 giờ liên tiếp.</summary>
    public const int MaxSlotsPerBooking = 4;

    /// <summary>Từ giờ này trở đi tính phụ thu giờ cao điểm.</summary>
    public const int PeakStartHour = 17;

    public const decimal PeakSurcharge = 50_000m;

    public const int MaxNoteLength = 500;

    /// <summary>Giá 1 khung giờ = giá cơ bản của sân + phụ thu (nếu là giờ cao điểm).</summary>
    public static decimal GetSlotPrice(decimal basePricePerHour, int startHour)
        => basePricePerHour + (startHour >= PeakStartHour ? PeakSurcharge : 0m);

    /// <summary>Tổng tiền = cộng giá từng khung giờ trong khoảng [startTime, endTime).</summary>
    public static decimal CalculateTotalPrice(decimal basePricePerHour, TimeOnly startTime, TimeOnly endTime)
    {
        decimal total = 0m;
        for (var hour = startTime.Hour; hour < endTime.Hour; hour++)
        {
            total += GetSlotPrice(basePricePerHour, hour);
        }

        return total;
    }

    /// <summary>
    /// Chuẩn hóa danh sách giờ bắt đầu người dùng chọn (bỏ trùng, sắp xếp) và kiểm tra:
    /// không rỗng, tối đa 4 khung, nằm trong giờ mở cửa, các khung liên tiếp nhau.
    /// </summary>
    public static bool TryNormalizeHours(IEnumerable<int>? input, out int[] hours, out string? error)
    {
        hours = input?.Distinct().OrderBy(h => h).ToArray() ?? Array.Empty<int>();
        error = null;

        if (hours.Length == 0)
        {
            error = "Chưa chọn khung giờ đặt sân.";
            return false;
        }

        if (hours.Length > MaxSlotsPerBooking)
        {
            error = $"Mỗi lượt đặt tối đa {MaxSlotsPerBooking} khung giờ liên tiếp.";
            return false;
        }

        if (hours[0] < OpeningHour || hours[^1] >= ClosingHour)
        {
            error = $"Sân chỉ nhận đặt từ {OpeningHour:00}:00 đến {ClosingHour:00}:00.";
            return false;
        }

        for (var i = 1; i < hours.Length; i++)
        {
            if (hours[i] != hours[i - 1] + 1)
            {
                error = "Vui lòng chọn các khung giờ liên tiếp nhau.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Kiểm tra khoảng thời gian đặt sân. Trả về null nếu hợp lệ, ngược lại trả về thông báo lỗi.
    /// <paramref name="nowVietnam"/> là giờ hiện tại theo giờ Việt Nam (xem <see cref="Common.VietnamClock"/>).
    /// </summary>
    public static string? ValidateTimeRange(DateOnly date, TimeOnly startTime, TimeOnly endTime, DateTime nowVietnam)
    {
        if (endTime <= startTime)
        {
            return "Giờ kết thúc phải lớn hơn giờ bắt đầu.";
        }

        if (startTime.Minute != 0 || startTime.Second != 0 || endTime.Minute != 0 || endTime.Second != 0)
        {
            return "Chỉ nhận đặt sân theo giờ tròn (ví dụ 17:00 - 19:00).";
        }

        if (startTime.Hour < OpeningHour || endTime.Hour > ClosingHour)
        {
            return $"Sân chỉ nhận đặt từ {OpeningHour:00}:00 đến {ClosingHour:00}:00.";
        }

        if (endTime.Hour - startTime.Hour > MaxSlotsPerBooking)
        {
            return $"Mỗi lượt đặt tối đa {MaxSlotsPerBooking} giờ liên tiếp.";
        }

        var today = DateOnly.FromDateTime(nowVietnam);
        if (date < today)
        {
            return "Không thể đặt sân cho ngày đã qua.";
        }

        if (date == today && startTime <= TimeOnly.FromDateTime(nowVietnam))
        {
            return "Khung giờ này đã qua, vui lòng chọn khung giờ sau.";
        }

        return null;
    }

    /// <summary>Khung giờ bắt đầu lúc <paramref name="startHour"/> đã trôi qua chưa (theo giờ Việt Nam)?</summary>
    public static bool IsSlotInPast(DateOnly date, int startHour, DateTime nowVietnam)
    {
        var today = DateOnly.FromDateTime(nowVietnam);
        if (date < today)
        {
            return true;
        }

        return date == today && new TimeOnly(startHour, 0) <= TimeOnly.FromDateTime(nowVietnam);
    }
}
