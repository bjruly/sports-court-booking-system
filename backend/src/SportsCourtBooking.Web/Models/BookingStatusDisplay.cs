using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

/// <summary>Nhãn và class CSS hiển thị trạng thái đơn đặt sân.</summary>
public static class BookingStatusDisplay
{
    public static string Label(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "Chờ xác nhận",
        BookingStatus.Confirmed => "Đã xác nhận",
        BookingStatus.Cancelled => "Đã hủy",
        BookingStatus.Completed => "Hoàn thành",
        _ => status.ToString()
    };

    public static string CssClass(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "status-badge--pending",
        BookingStatus.Confirmed => "status-badge--confirmed",
        BookingStatus.Cancelled => "status-badge--cancelled",
        BookingStatus.Completed => "status-badge--completed",
        _ => string.Empty
    };
}
