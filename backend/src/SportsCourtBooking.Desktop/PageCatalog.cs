using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Desktop.Pages;
using SportsCourtBooking.Desktop.Pages.Admin;

namespace SportsCourtBooking.Desktop;

public sealed record PageEntry(string Group, string Key, string Title, Func<AppServices, IShell, PageBase> Create);

/// <summary>Các màn hình của quản trị viên. Mục đầu tiên là màn hình mặc định.</summary>
public static class PageCatalog
{
    public static readonly IReadOnlyList<PageEntry> All = new PageEntry[]
    {
        new("TỔNG QUAN", "dashboard", "Thống kê hệ thống", (app, shell) => new AdminDashboardPage(app, shell)),
        new("ĐẶT SÂN", "bookings", "Lịch đặt theo ngày", (app, shell) => new DailyBookingsPage(app, shell)),
        new("ĐẶT SÂN", "refunds", "Theo dõi hoàn tiền", (app, shell) => new AdminRefundsPage(app, shell)),
        new("NGƯỜI DÙNG", "customers", "Khách hàng", (app, shell) => new CustomersPage(app, shell)),
        new("NGƯỜI DÙNG", "owners", "Chủ sân", (app, shell) => new OwnersPage(app, shell)),
        new("DANH MỤC", "facilities", "Cơ sở thể thao", (app, shell) => new AdminFacilitiesPage(app, shell)),
        new("DANH MỤC", "categories", "Môn thể thao & loại sân", (app, shell) => new CategoriesPage(app, shell)),
        new("DANH MỤC", "reviews", "Đánh giá vi phạm", (app, shell) => new AdminReviewsPage(app, shell)),
        new("BÁO CÁO", "reports", "Báo cáo tổng hợp", (app, shell) => new AdminReportsPage(app, shell)),
        new("BÁO CÁO", "category-report", "Báo cáo danh mục", (app, shell) => new CategoryReportPage(app, shell))
    };
}
