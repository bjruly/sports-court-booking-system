using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Web.Models;

public static class CourtStatusDisplay
{
    public static string Label(CourtStatus status) => status switch
    {
        CourtStatus.Active => "Đang hoạt động",
        CourtStatus.Maintenance => "Đang bảo trì",
        CourtStatus.Inactive => "Ngừng hoạt động",
        _ => status.ToString()
    };

    public static string CssClass(CourtStatus status) => status switch
    {
        CourtStatus.Active => "status-badge--active",
        CourtStatus.Maintenance => "status-badge--maintenance",
        CourtStatus.Inactive => "status-badge--inactive",
        _ => string.Empty
    };
}
