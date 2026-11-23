using System.Globalization;
using SportsCourtBooking.Application.Common;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Desktop.Core;

/// <summary>Nhãn tiếng Việt và cách định dạng dữ liệu dùng chung cho mọi màn hình.</summary>
public static class Display
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("vi-VN");

    public static string Money(decimal value) => value.ToString("N0", Culture) + " đ";

    public static string Date(DateOnly value) => value.ToString("dd/MM/yyyy", Culture);

    /// <summary>Thời điểm lưu UTC trong database, hiển thị theo giờ Việt Nam.</summary>
    public static string DateFromUtc(DateTime value)
        => VietnamClock.ToVietnamTime(value).ToString("dd/MM/yyyy", Culture);

    public static string DateTimeFromUtc(DateTime value)
        => VietnamClock.ToVietnamTime(value).ToString("dd/MM/yyyy HH:mm", Culture);

    public static string TimeRange(TimeOnly start, TimeOnly end)
        => $"{start.ToString("HH:mm", Culture)}–{end.ToString("HH:mm", Culture)}";

    public static string Percent(double value) => value.ToString("0.#", Culture) + "%";

    public static string Label(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "Chờ xác nhận",
        BookingStatus.Confirmed => "Đã xác nhận",
        BookingStatus.Cancelled => "Đã hủy",
        BookingStatus.Completed => "Hoàn thành",
        BookingStatus.Expired => "Hết hạn",
        _ => status.ToString()
    };

    public static string Label(CourtStatus status) => status switch
    {
        CourtStatus.Active => "Đang hoạt động",
        CourtStatus.Maintenance => "Đang bảo trì",
        CourtStatus.Inactive => "Ngừng hoạt động",
        _ => status.ToString()
    };

    public static string Label(RefundStatus? status) => status switch
    {
        RefundStatus.Pending => "Đang chờ chủ sân",
        RefundStatus.Completed => "Đã hoàn",
        RefundStatus.Failed => "Hoàn tiền thất bại",
        RefundStatus.Rejected => "Đã từ chối",
        _ => "Chưa yêu cầu"
    };

    public static string Label(PaymentStatus? status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid or PaymentStatus.DemoPaid => "Đã thanh toán",
        PaymentStatus.Refunded or PaymentStatus.DemoRefunded => "Đã hoàn tiền",
        PaymentStatus.Failed => "Thanh toán thất bại",
        _ => "Chưa có thanh toán"
    };

    public static Color Tone(BookingStatus status) => status switch
    {
        BookingStatus.Pending => Theme.Warning,
        BookingStatus.Confirmed => Theme.Info,
        BookingStatus.Completed => Theme.Success,
        BookingStatus.Cancelled => Theme.Danger,
        _ => Theme.Muted
    };

    public static Color Tone(CourtStatus status) => status switch
    {
        CourtStatus.Active => Theme.Success,
        CourtStatus.Maintenance => Theme.Warning,
        _ => Theme.Muted
    };

    public static Color Tone(RefundStatus status) => status switch
    {
        RefundStatus.Completed => Theme.Success,
        RefundStatus.Pending => Theme.Warning,
        _ => Theme.Danger
    };
}
