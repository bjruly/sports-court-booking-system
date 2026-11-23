using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Controls;
using SportsCourtBooking.Desktop.Core;

namespace SportsCourtBooking.Desktop.Pages.Admin;

public sealed class AdminDashboardPage : PageBase
{
    private readonly StatCard _courts = new("Tổng sân thể thao");
    private readonly StatCard _bookings = new("Tổng lượt đặt sân");
    private readonly StatCard _revenue = new("Doanh thu ghi nhận");
    private readonly StatCard _customers = new("Tổng khách hàng");
    private readonly DataGrid<CourtTypeReportDto> _sports = ReportGrids.Sports();
    private readonly SegmentBar _status = new();
    private readonly ColumnChart _daily = new();

    public AdminDashboardPage(AppServices app, IShell shell)
        : base(app, shell, "Tổng quan hệ thống", "Số liệu hoạt động của toàn hệ thống.")
    {
        Tool(Ui.Button("Báo cáo tổng hợp", (_, _) => Shell.Navigate("reports")));
        Tool(Ui.Button("Báo cáo danh mục", (_, _) => Shell.Navigate("category-report")));

        var status = Ui.Card();
        status.Dock = DockStyle.Fill;
        status.Controls.Add(_status);
        status.Controls.Add(SectionTitle("Trạng thái đơn đặt sân"));

        var daily = Ui.Card();
        daily.Dock = DockStyle.Fill;
        daily.Controls.Add(_daily);
        daily.Controls.Add(SectionTitle("Doanh thu 7 ngày gần nhất (đ)"));

        Body.Controls.Add(Split(
            GridCard(_sports, "Hiệu quả kinh doanh theo môn thể thao"),
            Stack(status, daily, 40),
            60));
        Body.Controls.Add(Gap());
        Body.Controls.Add(StatCard.Row(_courts, _bookings, _revenue, _customers));
    }

    protected override async Task LoadAsync(Func<bool> isCurrent)
    {
        var report = await App.CallAsync((IReportService service) => service.GetAdminReportAsync());
        if (!isCurrent())
        {
            return;
        }

        _courts.Set(report.TotalCourts.ToString(), $"{report.ActiveCourts} sân đang hoạt động");
        _bookings.Set(report.TotalBookings.ToString(),
            $"Thành công: {report.ConfirmedBookings} · Chờ: {report.PendingBookings}");
        _revenue.Set(Display.Money(report.TotalRevenue), $"Chờ duyệt: {Display.Money(report.PendingRevenue)}");
        _customers.Set(report.TotalCustomers.ToString(),
            $"Hoạt động: {report.ActiveCustomers} · Khóa: {report.LockedCustomers}");
        _sports.SetItems(report.CourtTypeReports);
        _status.SetSegments(
            new Segment("Thành công", report.ConfirmedBookings, Theme.ChartGreen),
            new Segment("Chờ duyệt", report.PendingBookings, Theme.ChartBlue),
            new Segment("Đã hủy / hết hạn", report.CancelledBookings, Theme.ChartOrange));
        _daily.SetPoints(report.RecentDailyRevenues
            .Select(day => new ChartPoint(
                day.Date.ToString("dd/MM", Display.Culture),
                day.Revenue,
                $"{Display.Date(day.Date)}: {Display.Money(day.Revenue)} · {day.BookingCount} đơn"))
            .ToList());
    }
}
