using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

public static class RefundStatusDisplay
{
    public static string Label(RefundStatus? status) => status switch
    {
        RefundStatus.Pending => "Đang chờ chủ sân",
        RefundStatus.Completed => "Đã hoàn",
        RefundStatus.Failed => "Thử hoàn thất bại",
        _ => "Chưa yêu cầu"
    };
}
